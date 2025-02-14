using System;
using System.Collections;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Variants;
using FieldDay;
using FieldDay.Components;
using FieldDay.Scenes;
using FieldDay.Scripting;
using Leaf.Runtime;
using UnityEngine;
using UnityEngine.Scripting;

namespace Pennycook {
    [RequireComponent(typeof(UnityEngine.BoxCollider))]
    public class ScriptCollider : ScriptActorComponent {
		
        #region Inspector
		private BoxCollider m_Collider = null;
        Vector3 m_OrigSize;
		Vector3 m_ReducedSize;
		#endregion // Inspector
		
        #region Leaf
		
		private void Awake() {
			if(m_Collider == null) {
				m_Collider = GetComponent<BoxCollider>();
				m_OrigSize = m_Collider.size;
				m_ReducedSize = m_Collider.size * 0.5f;
			}
        }

		[LeafMember("ReduceSize"), Preserve]
        public void ReduceSize() {
			m_Collider.size = m_ReducedSize;
        }
		
		[LeafMember("ResetSize"), Preserve]
        public void ResetSize() {
			m_Collider.size = m_OrigSize;
        }
        #endregion // Leaf
		
    }
}