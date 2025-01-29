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
    public class ScriptTexture : ScriptActorComponent {
		
        #region Inspector
		private MeshRenderer m_MeshRenderer = null;
        
        [SerializeField]
        private List<Texture> m_Textures = new List<Texture>();
		#endregion // Inspector
		
        #region Leaf
		
		private void Awake() {
			if(m_MeshRenderer == null) {
				m_MeshRenderer = GetComponent<MeshRenderer>();
			}
        }

		[LeafMember("SetEnabled"), Preserve]
        public void SetEnabled(bool enabled) {
			m_MeshRenderer.enabled = enabled;
        }

        [LeafMember("SetTexture"), Preserve]
        public void SetTexture(int index) {
            if(index < m_Textures.Count) {
                m_MeshRenderer.material.mainTexture = m_Textures[index];
            }
        }
		
        #endregion // Leaf
		
    }
}