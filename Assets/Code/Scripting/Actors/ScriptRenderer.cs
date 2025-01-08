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
    [RequireComponent(typeof(MeshRenderer))]
    public class ScriptRenderer : ScriptActorComponent {
		
        #region Inspector
		private MeshRenderer m_MeshRenderer = null;
        
        [SerializeField]
        private List<Material> m_Materials = new List<Material>();
		#endregion // Inspector
		
        #region Leaf
		
		private void Awake() {
			if(m_MeshRenderer == null) {
				m_MeshRenderer = GetComponent<MeshRenderer>();
			}
        }

        [LeafMember("SetMeshEnabled"), Preserve]
        public void SetMeshEnabled(bool lockParam) {
			m_MeshRenderer.enabled = lockParam;
        }

        [LeafMember("SetMaterial"), Preserve]
        public void SetMaterial(int index) {
            if(index < m_Materials.Count) {
                m_MeshRenderer.material = m_Materials[index];
            }
        }
		
        #endregion // Leaf
		
    }
}