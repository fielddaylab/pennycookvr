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
using UnityEngine.Rendering;
using UnityEngine.Scripting;

namespace Pennycook {
    
    public class ScriptSkybox : ScriptActorComponent {
		
        #region Inspector
		
        [SerializeField]
        private Material m_Skybox;
		#endregion // Inspector
		
        #region Leaf
		
		private void Awake() {
			
        }

        [LeafMember("SetSkybox"), Preserve]
        public void SetSkybox(float fFog) {
            RenderSettings.skybox = m_Skybox;
            RenderSettings.fogDensity = fFog;
        }
		
        #endregion // Leaf
		
    }
}