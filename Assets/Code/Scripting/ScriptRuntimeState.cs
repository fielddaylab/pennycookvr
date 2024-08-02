using System;
using BeauPools;
using BeauUtil;
using BeauUtil.Tags;
using BeauUtil.Variants;
using FieldDay.SharedState;
using Leaf.Runtime;

namespace FieldDay.Scripting {
    public class ScriptRuntimeState : SharedStateComponent, IRegistrationCallbacks {
        // Thread Tracking
        public LeafThreadHandle Cutscene;
        public readonly ScriptThreadMap ActorThreadMap = new ScriptThreadMap(32);
        public readonly RingBuffer<LeafThreadHandle> ActiveThreads = new RingBuffer<LeafThreadHandle>(16, RingBufferMode.Expand);

        // Actor Tracking
        public readonly ScriptActorMap Actors = new ScriptActorMap(16);

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
}