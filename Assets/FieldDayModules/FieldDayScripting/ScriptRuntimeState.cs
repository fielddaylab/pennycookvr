using System;
using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using BeauUtil.Tags;
using BeauUtil.Variants;
using FieldDay.SharedState;
using FieldDay.Vox;
using Leaf;
using Leaf.Runtime;
using UnityEngine;

namespace FieldDay.Scripting {
    public class ScriptRuntimeState : ISharedState, IRegistrationCallbacks {
        #region State

        // Thread Tracking
        internal LeafThreadHandle Cutscene;
        internal readonly ScriptThreadMap ActorThreadMap = new ScriptThreadMap(32);
        internal readonly RingBuffer<LeafThreadHandle> ActiveThreads = new RingBuffer<LeafThreadHandle>(16, RingBufferMode.Expand);

        // Actor Tracking
        internal readonly ScriptActorMap Actors = new ScriptActorMap(16);

        // Plugin
        internal ScriptPlugin Plugin;
        internal IMethodCache MethodCache;

        // Tag String
        internal CustomTagParserConfig TagParserConfig;
        internal TagStringEventHandler TagEventHandler;
        internal HashSet<StringHash32> SkippableTagEvents;
        internal HashSet<StringHash32> TextOutputTagEvents;

        // Pools
        internal IPool<ScriptThread> ThreadPool;
        internal IPool<VariantTable> TablePool;
        internal IPool<TagStringParser> ParserPool;

        // Variable Resolvers
        internal CustomVariantResolver Resolver;
        internal CustomVariantResolver ResolverOverride;

        // Randomization
        internal System.Random Random = new System.Random();

        // Execution State
        internal int PauseDepth;

        // current history buffer
        internal ScriptHistoryData CurrentHistoryBuffer;

        #endregion // State

        #region Callbacks

        public CastableEvent<ScriptThread, TagString> OnTaggedLineProcessed;

        #endregion // Callbacks

        #region IRegistrationCallbacks

        void IRegistrationCallbacks.OnDeregister() {
        }

        void IRegistrationCallbacks.OnRegister() {
            Resolver = new CustomVariantResolver();
            MethodCache = LeafUtils.CreateMethodCache(typeof(IScriptActorComponent));

            ResolverOverride = new CustomVariantResolver();
            ResolverOverride.Base = Resolver;

            ThreadPool = new DynamicPool<ScriptThread>(16, (p) => {
                return new ScriptThread(p, Plugin);
            });

            TablePool = new FixedPool<VariantTable>(16, Pool.DefaultConstructor<VariantTable>());
            TablePool.Config.RegisterOnAlloc((p, t) => t.Name = "temp");
            TablePool.Config.RegisterOnFree((p, t) => t.Reset());
            TablePool.Prewarm();
            
            ParserPool = new FixedPool<TagStringParser>(4, (p) => {
                var parser = new TagStringParser();
                parser.Delimiters = TagStringParser.CurlyBraceDelimiters;
                parser.EventProcessor = TagParserConfig;
                parser.ReplaceProcessor = TagParserConfig;
                return parser;
            });
            ParserPool.Prewarm();

            Plugin = new ScriptPlugin(this, ScriptUtility.DB);
            ThreadPool.Prewarm();

            CurrentHistoryBuffer = new ScriptHistoryData(64);
        }

        #endregion // IRegistrationCallbacks
    }

    static public partial class ScriptUtility {
        public const int RuntimeUpdateMask = 0x7FFFFFFF;

        [SharedStateReference] static public ScriptRuntimeState Runtime { get; private set; }
        [SharedStateReference] static public ScriptDatabase DB { get; private set; }

        [InvokePreBoot]
        static private void Initialize() {
            Game.SharedState.Register(new ScriptDatabase());
            Game.SharedState.Register(new ScriptRuntimeState());
            Game.Systems.Register(new ScriptRuntimeTickSystem());
        }

        #region Tables

        /// <summary>
        /// Binds a named variable table to the runtime.
        /// </summary>
        static public void BindTable(StringHash32 id, VariantTable table) {
            Runtime.Resolver.SetTable(id, table);
        }

        /// <summary>
        /// Removes a named variable table from the runtime.
        /// </summary>
        static public void UnbindTable(StringHash32 id) {
            Runtime.Resolver.ClearTable(id);
        }

        #endregion // Tables

        #region Tag Parsing

        /// <summary>
        /// Parses the given string into the given TagString.
        /// </summary>
        static public void ParseTag(ref TagString tagString, StringSlice line, object context = null) {
            TagStringParser parser = Runtime.ParserPool.Alloc();
            parser.Parse(ref tagString, line, context);
            Runtime.ParserPool.Free(parser);
        }

        /// <summary>
        /// Returns the character id embedded in the given line.
        /// </summary>
        static public StringHash32 GetCharacterId(TagString tagString) {
            tagString.TryFindEvent(LeafUtils.Events.Character, out var evtData);
            return evtData.Argument0.AsStringHash();
        }

        #endregion // Tag Parsing

        #region Actors

        /// <summary>
        /// Locates the actor for the given id.
        /// </summary>
        static public ILeafActor FindActor(StringHash32 actorId) {
            Runtime.Actors.TryGet(actorId, out ILeafActor actor);
            return actor;
        }

        #endregion // Actors

        #region Context

        static private LeafEvalContext GetEvalContext(ILeafActor actor, VariantTable table) {
            if (table == null || table.Count == 0) {
                return LeafEvalContext.FromResolver(Runtime.Plugin, Runtime.Resolver, actor);
            }

            Runtime.ResolverOverride.SetDefaultTable(table);
            return LeafEvalContext.FromResolver(Runtime.Plugin, Runtime.ResolverOverride, actor);
        }

        #endregion // Context

        #region Functions

        static public void Invoke(StringHash32 functionId, VariantTable vars = null) {
            Invoke(functionId, default, null, vars);
        }

        static public void Invoke(StringHash32 functionId, ILeafActor actor, VariantTable vars = null) {
            Invoke(functionId, actor?.Id ?? StringHash32.Null, actor, vars);
        }

        static public void Invoke(StringHash32 functionId, StringHash32 targetId, ILeafActor actor, VariantTable vars = null) {
            using (PooledList<ScriptNode> funcNodes = PooledList<ScriptNode>.Create()) {
                ScriptNodeLookupArgs lookup;
                lookup.TargetId = targetId;
                lookup.History = Runtime.CurrentHistoryBuffer;
                lookup.Randomizer = Runtime.Random;
                lookup.ThreadMap = Runtime.ActorThreadMap;
                lookup.CurrentlyInCutsceneOrBlockingState = Runtime.Cutscene.IsRunning();
                lookup.CurrentTime = Time.time;
                lookup.EvalContext = GetEvalContext(actor, vars);
                ScriptDatabaseUtility.FindAllFunctions(DB, functionId, lookup, funcNodes);
                foreach (var node in funcNodes) {
                    Runtime.Plugin.Run(node, targetId, actor, vars, "Function Invokation", true);
                }
            }
        }

        #endregion // Functions

        #region Trigger

        static public LeafThreadHandle Trigger(StringHash32 triggerId, VariantTable vars = null) {
            return Trigger(triggerId, default, null, vars);
        }

        static public LeafThreadHandle Trigger(StringHash32 triggerId, ILeafActor actor, VariantTable vars = null) {
            return Trigger(triggerId, actor?.Id ?? StringHash32.Null, actor, vars);
        }

        static public LeafThreadHandle Trigger(StringHash32 triggerId, StringHash32 targetId, ILeafActor actor, VariantTable vars = null) {
            Invoke(triggerId, targetId, actor, vars);

            ScriptNodeLookupArgs lookup;
            lookup.TargetId = targetId;
            lookup.History = Runtime.CurrentHistoryBuffer;
            lookup.Randomizer = Runtime.Random;
            lookup.ThreadMap = Runtime.ActorThreadMap;
            lookup.CurrentlyInCutsceneOrBlockingState = Runtime.Cutscene.IsRunning();
            lookup.CurrentTime = Time.time;
            lookup.EvalContext = GetEvalContext(actor, vars);

            ScriptNode node = ScriptDatabaseUtility.FindRandomTrigger(DB, triggerId, lookup);
            if (node != null) {
                return Runtime.Plugin.Run(node, targetId, actor, vars, "Trigger Invokation", true);
            }

            return default;
        }

        #endregion // Trigger

        #region Vox

        static internal VoxPriority ScriptPriorityToVoxPriority(ScriptNodePriority priority) {
            return (VoxPriority) priority;
        }

        #endregion // Vox
    }
}