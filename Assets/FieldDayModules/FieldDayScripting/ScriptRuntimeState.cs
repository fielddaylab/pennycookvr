using System;
using BeauPools;
using BeauUtil;
using BeauUtil.Tags;
using BeauUtil.Variants;
using FieldDay.SharedState;
using Leaf;
using Leaf.Runtime;

namespace FieldDay.Scripting {
    public class ScriptRuntimeState : ISharedState, IRegistrationCallbacks {
        // Thread Tracking
        public LeafThreadHandle Cutscene;
        public readonly ScriptThreadMap ActorThreadMap = new ScriptThreadMap(32);
        public readonly RingBuffer<LeafThreadHandle> ActiveThreads = new RingBuffer<LeafThreadHandle>(16, RingBufferMode.Expand);

        // Actor Tracking
        public readonly ScriptActorMap Actors = new ScriptActorMap(16);

        // Plugin
        public ILeafPlugin<ScriptNode> Plugin;

        // Pools
        public IPool<ScriptThread> ThreadPool;
        public IPool<VariantTable> TablePool;
        public IPool<TagStringParser> ParserPool;

        // Variable Resolvers
        public CustomVariantResolver Resolver;
        public CustomVariantResolver ResolverOverride;

        // Randomization
        public System.Random Random = new Random();

        #region IRegistrationCallbacks

        void IRegistrationCallbacks.OnDeregister() {
        }

        void IRegistrationCallbacks.OnRegister() {
            
        }

        #endregion // IRegistrationCallbacks
    }

    static public partial class ScriptUtility {
        [SharedStateReference] static public ScriptRuntimeState Runtime { get; private set; }
        [SharedStateReference] static public ScriptDatabase DB { get; private set; }

        [InvokePreBoot]
        static public void Initialize() {
            Game.SharedState.Register(new ScriptDatabase());
            Game.SharedState.Register(new ScriptRuntimeState());
        }
    }
}