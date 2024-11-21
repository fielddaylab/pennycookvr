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
    [RequireComponent(typeof(TMPro.TextMeshProUGUI))]
    public class ScriptText : ScriptActorComponent {
		
        #region Inspector
		private TMPro.TextMeshProUGUI m_TextRenderer = null;
        
		#endregion // Inspector
		
        #region Leaf
		
		private void Awake() {
			if(m_TextRenderer == null) {
				m_TextRenderer = GetComponent<TMPro.TextMeshProUGUI>();
			}
        }
		
        [LeafMember("SetText"), Preserve]
        void SetText(string s)
        {
            m_TextRenderer.text = s;
        }
        #endregion // Leaf
		
    }
}