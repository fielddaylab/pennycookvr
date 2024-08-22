using System;
using System.Collections;
using System.Collections.Generic;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Tags;
using BeauUtil.Variants;
using FieldDay.SharedState;
using Leaf;
using Leaf.Defaults;
using Leaf.Runtime;
using UnityEngine;
using static BeauRoutine.Routine;

namespace FieldDay.Scripting {
    public class ScriptPlugin : ILeafPlugin<ScriptNode>, ILeafPlugin, ILeafVariableAccess {
        private readonly ScriptRuntimeState m_RuntimeState;
        private readonly ScriptDatabase m_Database;
        private readonly IMethodCache m_CachedMethodCache;
        private readonly IVariantResolver m_CachedResolver;
        private readonly LeafRuntimeConfiguration m_Configuration;

        public ScriptPlugin(ScriptRuntimeState runtimeState, ScriptDatabase database) {
            m_RuntimeState = runtimeState;
            m_Database = database;

            m_CachedMethodCache = runtimeState.MethodCache;
            m_CachedResolver = runtimeState.Resolver;
            
            m_Configuration = new LeafRuntimeConfiguration();
        }

        #region Tracking

        internal void StopTracking(ScriptThread threadState) {
            LeafThreadHandle handle = threadState.GetHandle();

            if (m_RuntimeState.Cutscene == handle) {
                m_RuntimeState.Cutscene = default;
            }

            StringHash32 who = threadState.Target();
            if (!who.IsEmpty) {
                if (m_RuntimeState.ActorThreadMap.Threads.TryGetValue(who, out var recordedHandle) && handle == recordedHandle) {
                    m_RuntimeState.ActorThreadMap.Threads.Remove(who);
                }
            }

            m_RuntimeState.ActiveThreads.FastRemove(handle);
        }

        #endregion // Tracking

        #region Running

        public LeafThreadState<ScriptNode> Fork(LeafThreadState<ScriptNode> inParentThreadState, ScriptNode inForkNode) {
            ScriptThread thread = (ScriptThread) inParentThreadState;
            var handle = Run(inForkNode, thread.Target(), thread.Actor, thread.Locals, null, true);
            return handle.GetThread<ScriptThread>();
        }

        public LeafThreadHandle Run(ScriptNode node, StringHash32 targetId, ILeafActor actor, VariantTable localVars, string name, bool tickImmediately) {
            if (node == null) {
                return default(LeafThreadHandle);
            }

            if ((node.Flags & ScriptNodeFlags.Cutscene) != 0) {
                m_RuntimeState.Cutscene.Kill();
            }

            if (!targetId.IsEmpty && m_RuntimeState.ActorThreadMap.Threads.TryGetValue(targetId, out LeafThreadHandle existingActorThread)) {
                existingActorThread.Kill();
            }

            ScriptThread threadState = m_RuntimeState.ThreadPool.Alloc();
            LeafThreadHandle threadHandle = threadState.Setup(name, actor, localVars);
            threadState.SetInitialNode(node, targetId);
            threadState.AttachRoutine(Routine.Start(GameLoop.Host, LeafRuntime.Execute(threadState, node)).SetPhase(RoutinePhase.Manual));

            if (!targetId.IsEmpty) {
                m_RuntimeState.ActorThreadMap.Threads[targetId] = threadHandle;
            }

            m_RuntimeState.ActiveThreads.PushBack(threadHandle);

            if (tickImmediately) {
                threadState.ForceTick();
            }

            return threadHandle;
        }

        #endregion // Running

        #region Node Flow

        public void OnNodeEnter(ScriptNode inNode, LeafThreadState<ScriptNode> inThreadState) {
            ScriptThread thread = (ScriptThread) inThreadState;
            
            inNode.Package().AddReference();

            m_RuntimeState.CurrentHistoryBuffer?.RecordVisit(inNode.Id(), inNode.PersistenceScope, Time.realtimeSinceStartup);

            if ((inNode.Flags & ScriptNodeFlags.Cutscene) != 0) {
                thread.PushCutscene();
            }
        }

        public void OnNodeExit(ScriptNode inNode, LeafThreadState<ScriptNode> inThreadState) {
            ScriptThread thread = (ScriptThread) inThreadState;

            inNode.Package().ReleaseReference();

            if ((inNode.Flags & ScriptNodeFlags.Cutscene) != 0) {
                thread.PopCutscene();
            }
        }

        public void OnEnd(LeafThreadState<ScriptNode> inThreadState) {
            ScriptThread thread = (ScriptThread) inThreadState;
            thread.Kill();
        }

        internal void SetCutscene(LeafThreadHandle handle) {
            if (m_RuntimeState.Cutscene != handle) {
                m_RuntimeState.Cutscene.Kill();
                m_RuntimeState.Cutscene = handle;
            }
        }

        internal void DereferenceCutscene(LeafThreadHandle handle) {
            if (m_RuntimeState.Cutscene == handle) {
                m_RuntimeState.Cutscene = default;
            }
        }

        #endregion // Node Flow

        #region Operation

        public IEnumerator RunLine(LeafThreadState<ScriptNode> inThreadState, LeafLineInfo inLine) {
            ScriptThread thread = (ScriptThread) inThreadState;
            if (thread.IsSkipping()) {
                if (LeafRuntime.PredictChoice(thread)) {
                    thread.StopSkipping();
                } else {
                    SkipLine(thread, inLine);
                    return null;
                }
            }

            return ExecuteLine(thread, inLine);
        }

        protected virtual void SkipLine(ScriptThread thread, LeafLineInfo line) {
            if (line.IsEmptyOrWhitespace) {
                return;
            }

            TagString str = thread.TagString;
            ScriptUtility.ParseTag(ref str, line.Text, thread);
            m_RuntimeState.OnTaggedLineProcessed.Invoke(thread, str);

            TagStringEventHandler evtHandler = m_RuntimeState.TagEventHandler;
            var nodes = str.Nodes;
            for(int i = 0; i < nodes.Length; i++) {
                var node = nodes[i];
                if (node.Type == TagNodeType.Event && !m_RuntimeState.SkippableTagEvents.Contains(node.Event.Type)) {
                    evtHandler.TryEvaluate(node.Event, thread, out IEnumerator coroutine);
                    if (coroutine != null) {
                        Log.Warn("[ScriptPlugin] Coroutine '{0}' generated for event '{1}' during a skip - this coroutine will not be processed", coroutine.ToString(), node.Event.Type.ToDebugString());
                        (coroutine as IDisposable)?.Dispose();
                    }
                }
            }
        }

        protected virtual IEnumerator ExecuteLine(ScriptThread thread, LeafLineInfo line) {
            if (line.IsEmptyOrWhitespace) {
                yield return null;
            }

            TagString str = thread.TagString;
            ScriptUtility.ParseTag(ref str, line.Text, thread);
            m_RuntimeState.OnTaggedLineProcessed.Invoke(thread, str);

            // TODO: handle evaluating if dialog box is required


        }

        #endregion // Operation

        #region Choice

        public IEnumerator ShowOptions(LeafThreadState<ScriptNode> inThreadState, LeafChoice inChoice) {
            return ExecuteChoice((ScriptThread) inThreadState, inChoice);
        }

        protected virtual IEnumerator ExecuteChoice(ScriptThread thread, LeafChoice choice) {
            // TODO: Implement
            throw new NotImplementedException();
        }

        #endregion // Choice

        #region Lookups

        public bool TryLookupObject(StringHash32 inObjectId, LeafThreadState inThreadState, out object outObject) {
            bool result = m_RuntimeState.Actors.TryGet(inObjectId, out ILeafActor actor);
            outObject = actor;
            return result;
        }

        public bool TryLookupLine(StringHash32 inLineCode, LeafNode inLocalNode, out string outLine) {
            // TODO: if non-default language, lookup from localization instead
            outLine = null;
            return false;
        }

        public bool TryLookupNode(StringHash32 inNodeId, ScriptNode inLocalNode, out ScriptNode outLeafNode) {
            return ScriptDatabaseUtility.TryLookupNode(m_Database, inLocalNode, inNodeId, out outLeafNode);
        }

        #endregion // Lookups

        #region ILeafPlugin

        IMethodCache ILeafPlugin.MethodCache {
            get { return m_CachedMethodCache; }
        }

        public LeafRuntimeConfiguration Configuration {
            get { return m_Configuration; }
        }

        int ILeafPlugin.RandomInt(int inMin, int inMaxExclusive) {
            return m_RuntimeState.Random.Next(inMin, inMaxExclusive);
        }

        float ILeafPlugin.RandomFloat(float inMin, float inMax) {
            return m_RuntimeState.Random.NextFloat(inMin, inMax);
        }

        #endregion // ILeafPlugin

        #region ILeafVariableAccess

        IVariantResolver ILeafVariableAccess.Resolver {
            get { return m_CachedResolver; }
        }

        #endregion // ILeafVariableAccess
    }
}