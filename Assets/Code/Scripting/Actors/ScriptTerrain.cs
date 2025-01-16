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
    [RequireComponent(typeof(Terrain))]
    public class ScriptTerrain : ScriptActorComponent {
		
        #region Inspector
		private Terrain m_Terrain = null;
        
        [SerializeField]
        private List<Material> m_Materials = new List<Material>();
		#endregion // Inspector
		
        #region Leaf
		
		private void Awake() {
			if(m_Terrain == null) {
				m_Terrain = GetComponent<Terrain>();
			}
        }

        [LeafMember("SetTerrainMaterial"), Preserve]
        public void SetTerrainMaterial(int index) {
            if(index < m_Materials.Count) {
                m_Terrain.materialTemplate = m_Materials[index];
            }
        }
		
        #endregion // Leaf
		
    }
}