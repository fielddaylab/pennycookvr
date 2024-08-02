using System;
using BeauPools;
using BeauUtil;
using BeauUtil.Variants;
using FieldDay.Components;
using Leaf.Runtime;
using UnityEngine;

namespace FieldDay.Scripting {
    public class ScriptActor : BatchedComponent, IPoolAllocHandler, IPoolConstructHandler, ILeafActor {
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
            
        }

        void IPoolAllocHandler.OnFree() {
            
        }

        void IPoolConstructHandler.OnDestruct() {
            
        }

        #endregion // Pool Callbacks
    }
}