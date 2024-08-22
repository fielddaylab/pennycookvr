#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#define HOT_RELOAD_SUPPORTED
#endif // DEVELOPMENT

using System;
using System.Collections.Generic;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.IO;
using BeauUtil.Variants;
using FieldDay.Data;
using FieldDay.Debugging;
using FieldDay.Scenes;
using FieldDay.SharedState;
using Leaf;
using Leaf.Runtime;
using UnityEngine;

namespace FieldDay.Scripting {
    [DisallowMultipleComponent]
    public sealed class ScriptDatabase : ISharedState, ISceneLoadDependency, IRegistrationCallbacks {
        public const int MaxLoadedPackages = 16;

        // loaded
        public HashSet<ScriptNodePackage> RegisteredPackages = new HashSet<ScriptNodePackage>(MaxLoadedPackages);
        public ScriptNodePackage[] LoadedHandleMap = new ScriptNodePackage[MaxLoadedPackages];
        public Dictionary<StringHash32, NodeBucket<ScriptNode>> LoadedNodeBuckets = MapUtils.Create<StringHash32, NodeBucket<ScriptNode>>(8);
        public Dictionary<StringHash32, ScriptNode> LoadedExposedNodes = MapUtils.Create<StringHash32, ScriptNode>(16);

        // loading
        public UniqueIdAllocator16 HandleGenerator = new UniqueIdAllocator16(MaxLoadedPackages);
        public RingBuffer<ScriptDatabaseLoadRequest> LoadQueue = new RingBuffer<ScriptDatabaseLoadRequest>();
        [NonSerialized] public ScriptDatabaseLoadRequest CurrentLoadRequest;
        [NonSerialized] public AsyncHandle CurrentLoadHandle;

        // unload
        public RingBuffer<UniqueId16> UnloadQueue = new RingBuffer<UniqueId16>();

        #region ISceneLoadDepencency

        public bool IsLoaded(SceneLoadPhase loadPhase) {
            if ((loadPhase & SceneLoadPhase.BeforeReady) != 0) {
                return LoadQueue.Count == 0 && !CurrentLoadHandle.IsRunning();
            }
            return !CurrentLoadHandle.IsRunning();
        }

        #endregion // ISceneLoadDependency

        #region IRegistrationCallbacks

        void IRegistrationCallbacks.OnDeregister() {
            Game.Scenes?.DeregisterLoadDependency(this);
        }

        void IRegistrationCallbacks.OnRegister() {
            Game.Scenes.RegisterLoadDependency(this);
        }

        #endregion // IRegistrationCallbacks
    }

    public struct ScriptDatabaseLoadRequest {
        public ReloadableRef<LeafAsset> Asset;
        public ScriptNodePackage Package;
        public UniqueId16 Handle;
    }

    static public class ScriptDatabaseUtility {
        #region Loading

        //static public UniqueId16 Load(ScriptDatabase db, LeafAsset asset) {
            
        //}

        //static public UniqueId16 LoadNow(ScriptDatabase db, LeafAsset asset) {

        //}

        //static public void Unload(ScriptDatabase db, UniqueId16 id) {
        //    if (!db.HandleGenerator.IsValid(id)) {
        //        Log.Trace("[ScriptDatabase] Invalid/expired handle");
        //        return;
        //    }
            
        //    if (CancelCurrentLoad(db, id)) {
        //        return;
        //    }

        //    ScriptNodePackage package = db.LoadedHandleMap[id.Index];
        //    if (package.SetActive(false)) {
        //        db.UnloadQueue.PushBack(id);
        //        Log.Trace("[ScriptDatabase] Queueing script unload '{0}'", package.Name());
        //    }
        //}

        //static private bool CancelCurrentLoad(ScriptDatabase db, LeafAsset asset, out UniqueId16 prevHandle) {
        //    if (ReferenceEquals(db.CurrentLoadRequest.Asset, asset)) {
        //        prevHandle = db.CurrentLoadRequest.Handle;
        //        db.CurrentLoadRequest.Package.Clear();
        //        db.CurrentLoadHandle.Cancel();
        //        db.CurrentLoadRequest = default;
        //        db.HandleGenerator.Free(prevHandle);
        //        return true;
        //    } else {
        //        prevHandle = default;
        //        return false;
        //    }
        //}

        //static private bool CancelCurrentLoad(ScriptDatabase db, UniqueId16 handle) {
        //    if (db.CurrentLoadRequest.Handle == handle) {
        //        db.CurrentLoadRequest.Package.Clear();
        //        db.CurrentLoadHandle.Cancel();
        //        db.CurrentLoadRequest = default;
        //        db.HandleGenerator.Free(handle);
        //        return true;
        //    } else {
        //        return false;
        //    }
        //}

        #endregion // Loading

        #region Package Registration

        #endregion // Package Registration

        #region Lookups

        static private readonly WeightedSet<ScriptNode> s_WeightedWorkList = new WeightedSet<ScriptNode>(16);

        /// <summary>
        /// Finds a node with the given id, searching from an existing node's scope first,
        /// if provided, and then to any nodes marked with `@exposed`
        /// </summary>
        static public bool TryLookupNode(ScriptDatabase db, ScriptNode scope, StringHash32 nodeId, out ScriptNode node) {
            if (scope != null && scope.Package().TryGetNode(nodeId, out node)) {
                return true;
            }

            return db.LoadedExposedNodes.TryGetValue(nodeId, out node);
        }

        /// <summary>
        /// Finds a random valid trigger for the given bucket and request.
        /// </summary>
        static public unsafe ScriptNode FindRandomTrigger(ScriptDatabase db, StringHash32 bucketId, ScriptNodeLookupArgs request) {
            NodeBucket<ScriptNode> bucket;
            if (db.LoadedNodeBuckets.TryGetValue(bucketId, out bucket)) {
                if (NodeBucketUtility.EnsureSorted(ref bucket)) {
                    db.LoadedNodeBuckets[bucketId] = bucket;
                }
                using(PooledList<ScriptNode> lookupList = PooledList<ScriptNode>.Create()) {
#if ENABLE_IL2CPP
                    int count = NodeBucketUtility.GetHighestScoringSorted(ref bucket, request, &IsValidTrigger, lookupList);
#else
                    int count = NodeBucketUtility.GetHighestScoringSorted(ref bucket, request, IsValidTrigger, lookupList);
#endif // ENABLE_IL2CPP
                    if (count == 1) {
                        return lookupList[0];
                    } else if (count > 1) {
                        bool weightedSelection = false;
                        foreach(var node in lookupList) {
                            if ((node.Flags & ScriptNodeFlags.IsWeighted) != 0) {
                                weightedSelection = true;
                                break;
                            }
                        }

                        if (weightedSelection) {
                            s_WeightedWorkList.Clear();
                            foreach(var node in lookupList) {
                                s_WeightedWorkList.Add(node, node.SelectionWeight);
                            }
                            s_WeightedWorkList.Cache();
                            ScriptNode weightedChoice = s_WeightedWorkList.GetItem(request.Randomizer);
                            s_WeightedWorkList.Clear();

                            return weightedChoice;
                        } else {
                            return request.Randomizer.Choose(lookupList);
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Finds all valid functions for the given bucket and request.
        /// </summary>
        static public unsafe int FindAllFunctions(ScriptDatabase db, StringHash32 bucketId, ScriptNodeLookupArgs request, ICollection<ScriptNode> functions) {
            NodeBucket<ScriptNode> bucket;
            if (db.LoadedNodeBuckets.TryGetValue(bucketId, out bucket)) {
#if ENABLE_IL2CPP
                return NodeBucketUtility.GetAllUnsorted(ref bucket, request, &IsValidFunction, functions);
#else
                return NodeBucketUtility.GetAllUnsorted(ref bucket, request, IsValidFunction, functions);
#endif // ENABLE_IL2CPP
            } else {
                return 0;
            }
        }

        #endregion // Lookups

        #region Checking

        static private bool IsValidTrigger(ScriptNode node, ScriptNodeLookupArgs request) {
            if (DebugFlags.IsFlagSet(ScriptDebugFlags.LogNodeEvaluation)) {
                Log.Trace("[ScriptDatabase] Evaluating trigger node '{0}'...", node.FullName);
            }

            if (!node.Package().IsActive()) {
                if (DebugFlags.IsFlagSet(ScriptDebugFlags.LogNodeEvaluation)) {
                    Log.Trace("...node package '{0}' is unloading/inactive", node.Package().Name());
                }
                return false;
            }

            // if not the right target
            if ((node.Flags & ScriptNodeFlags.AnyTarget) == 0 && !request.TargetId.IsEmpty && request.TargetId != node.TargetId) {
                if (DebugFlags.IsFlagSet(ScriptDebugFlags.LogNodeEvaluation)) {
                    Log.Trace("...node has mismatched target '{0}' vs desired '{1}'", node.TargetId.ToDebugString(), request.TargetId.ToDebugString());
                }
                return false;
            }

            if (request.BlockCutscenes) {
                // if set to ignore during a cutscene while cutscene is ongoing
                if ((node.Flags & ScriptNodeFlags.IgnoreDuringCutscene) != 0) {
                    if (DebugFlags.IsFlagSet(ScriptDebugFlags.LogNodeEvaluation)) {
                        Log.Trace("...cutscene is playing");
                    }
                    return false;
                }

                // if cutscene, and cutscene playing, but interrupt not set
                if ((node.Flags & ScriptNodeFlags.Cutscene) != 0 && (node.Flags & ScriptNodeFlags.InterruptSamePriority) == 0) {
                    if (DebugFlags.IsFlagSet(ScriptDebugFlags.LogNodeEvaluation)) {
                        Log.Trace("...cutscene is playing and cutscene node is not able to interrupt");
                    }
                    return false;
                }
            }

            // if set to be once
            if ((node.Flags & ScriptNodeFlags.Once) != 0 && request.History.HasSeen(node.Id(), node.PersistenceScope)) {
                if (DebugFlags.IsFlagSet(ScriptDebugFlags.LogNodeEvaluation)) {
                    Log.Trace("...node has already been visited");
                }
                return false;
            }

            if ((node.Flags & ScriptNodeFlags.HasTimeCooldown) == 0) {
                // if has been seen within the repeat cooldown
                if (node.RepeatPeriod.NodeWindow > 0 && request.History.HasRecentlySeen(node.Id(), node.RepeatPeriod.NodeWindow)) {
                    if (DebugFlags.IsFlagSet(ScriptDebugFlags.LogNodeEvaluation)) {
                        Log.Trace("...node has already been visited within a {0}-node window", node.RepeatPeriod.NodeWindow);
                    }
                    return false;
                }
            } else {
                // if has been seen within the repeat time cooldown
                if (node.RepeatPeriod.TimeWindow > 0 && request.History.HasRecentlySeenAfterTimestamp(node.Id(), request.CurrentTime - node.RepeatPeriod.TimeWindow)) {
                    if (DebugFlags.IsFlagSet(ScriptDebugFlags.LogNodeEvaluation)) {
                        Log.Trace("...node has already been visited within a {0}-second window", node.RepeatPeriod.TimeWindow);
                    }
                    return false;
                }
            }

            // if target is playing a node that this node cannot interrupt
            StringHash32 targetId = request.TargetId.IsEmpty ? ((node.Flags & ScriptNodeFlags.AnyTarget) == 0 ? node.TargetId : default(StringHash32)) : request.TargetId;
            if (!targetId.IsEmpty) {
                ScriptThread currentThread = request.ThreadMap.GetThread(targetId);
                if (currentThread != null) {
                    bool canInterrupt;
                    if ((node.Flags & ScriptNodeFlags.InterruptSamePriority) != 0) {
                        canInterrupt = currentThread.Priority() <= node.Priority;
                    } else {
                        canInterrupt = currentThread.Priority() < node.Priority;
                    }

                    if (!canInterrupt) {
                        if (DebugFlags.IsFlagSet(ScriptDebugFlags.LogNodeEvaluation)) {
                            Log.Trace("...higher-priority node '{0}' is executing for target '{1}'", currentThread.InitialNodeId().ToDebugString(), targetId.ToDebugString());
                        }
                        return false;
                    }
                }
            }

            // conditions
            if (node.Conditions.Count > 0) {
                LeafExpression failure;
                Variant result = node.Conditions.Evaluate(request.EvalContext, out failure);
                if (!result.AsBool()) {
                    if (DebugFlags.IsFlagSet(ScriptDebugFlags.LogNodeEvaluation)) {
                        Log.Trace("...node condition '{0}' failed", failure.ToDebugString(node));
                    }
                    return false;
                }
            }

            if (DebugFlags.IsFlagSet(ScriptDebugFlags.LogNodeEvaluation)) {
                Log.Trace("...node passed!");
            }
            return true;
        }

        static private bool IsValidFunction(ScriptNode node, ScriptNodeLookupArgs request) {
            if (DebugFlags.IsFlagSet(ScriptDebugFlags.LogNodeEvaluation)) {
                Log.Trace("[ScriptDatabase] Evaluating function node '{0}'...", node.FullName);
            }

            // unloading package
            if (!node.Package().IsActive()) {
                if (DebugFlags.IsFlagSet(ScriptDebugFlags.LogNodeEvaluation)) {
                    Log.Trace("...node package '{0}' is unloading/inactive", node.Package().Name());
                }
                return false;
            }

            // if not the right target
            if ((node.Flags & ScriptNodeFlags.AnyTarget) == 0 && !request.TargetId.IsEmpty && request.TargetId != node.TargetId) {
                if (DebugFlags.IsFlagSet(ScriptDebugFlags.LogNodeEvaluation)) {
                    Log.Trace("...node has mismatched target '{0}' vs desired '{1}'", node.TargetId.ToDebugString(), request.TargetId.ToDebugString());
                }
                return false;
            }

            if (request.BlockCutscenes) {
                // if set to ignore during a cutscene while cutscene is ongoing
                if ((node.Flags & ScriptNodeFlags.IgnoreDuringCutscene) != 0) {
                    if (DebugFlags.IsFlagSet(ScriptDebugFlags.LogNodeEvaluation)) {
                        Log.Trace("...cutscene is playing");
                    }
                    return false;
                }
            }

            // if set to be once
            if ((node.Flags & ScriptNodeFlags.Once) != 0 && request.History.HasSeen(node.Id(), node.PersistenceScope)) {
                if (DebugFlags.IsFlagSet(ScriptDebugFlags.LogNodeEvaluation)) {
                    Log.Trace("...node has already been visited");
                }
                return false;
            }

            // conditions
            if (node.Conditions.Count > 0) {
                LeafExpression failure;
                Variant result = node.Conditions.Evaluate(request.EvalContext, out failure);
                if (!result.AsBool()) {
                    if (DebugFlags.IsFlagSet(ScriptDebugFlags.LogNodeEvaluation)) {
                        Log.Trace("...node condition '{0}' failed", failure.ToDebugString(node));
                    }
                    return false;
                }
            }

            if (DebugFlags.IsFlagSet(ScriptDebugFlags.LogNodeEvaluation)) {
                Log.Trace("...node passed!");
            }
            return true;
        }

        #endregion // Checking
    }

    public struct ScriptNodeLookupArgs {
        public LeafEvalContext EvalContext;
        public StringHash32 TargetId;
        public bool BlockCutscenes;
        public ScriptThreadMap ThreadMap;
        public ScriptHistoryData History;
        public float CurrentTime;

        public System.Random Randomizer;
    }
}