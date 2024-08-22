using System;
using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using BeauUtil.Tags;
using BeauUtil.Variants;
using FieldDay.SharedState;
using Leaf;
using Leaf.Runtime;

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
        internal ILeafPlugin<ScriptNode> Plugin;
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
        internal System.Random Random = new Random();

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

        #endregion // Tag Parsing
    }
}