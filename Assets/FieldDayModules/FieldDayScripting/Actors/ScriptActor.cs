using System;
using BeauPools;
using BeauUtil;
using BeauUtil.Variants;
using FieldDay.Components;
using Leaf.Runtime;
using UnityEngine;

namespace FieldDay.Scripting {
    public class ScriptActor : BatchedComponent, IPoolAllocHandler, IPoolConstructHandler, ILeafActor, IRegistrationCallbacks {
        #region Inspector

        [SerializeField] private SerializedHash32 m_Id = string.Empty;

        #endregion // Inspector

        [NonSerialized] private VariantTable m_Locals;
        [NonSerialized] private bool m_Pooled;

        #region ILeafActor

        public StringHash32 Id { get { return m_Id.Hash(); } }
        public VariantTable Locals { get { return m_Locals; } }

        #endregion // ILeafActor

        #region Pool Callbacks

        void IPoolConstructHandler.OnConstruct() {
            m_Pooled = true;
        }

        void IPoolAllocHandler.OnAlloc() {
            Register(this);
        }

        void IPoolAllocHandler.OnFree() {
            Deregister(this);
        }

        void IPoolConstructHandler.OnDestruct() {
            Deregister(this);
            
        }

        #endregion // Pool Callbacks

        #region Unity Events

        private void OnDestroy() {
            Deregister(this);
        }

        #endregion // Unity Events

        #region IRegistrationCallbacks

        void IRegistrationCallbacks.OnRegister() {
            Register(this);
        }

        void IRegistrationCallbacks.OnDeregister() {
        }

        #endregion // IRegistrationCallbacks

        #region Registration

        static public bool Register(ScriptActor actor) {
            ScriptRuntimeState runtime = Find.State<ScriptRuntimeState>();
            if (runtime.Actors.Register(actor)) {
                return true;
            }

            return false;
        }

        static public bool Deregister(ScriptActor actor) {
            if (Game.IsShuttingDown) {
                return false;
            }

            ScriptRuntimeState runtime = Find.State<ScriptRuntimeState>();
            if (runtime.Actors.Deregister(actor)) {
                return true;
            }
            return false;
        }

        #endregion // Registration
    }
}