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
    [RequireComponent(typeof(UnityEngine.Animation))]
    public class ScriptAnimation : ScriptActorComponent {
		
        #region Inspector
		private UnityEngine.Animation m_Anim = null;
        
		#endregion // Inspector
		
        #region Leaf
		
		private void Awake() {
			if(m_Anim == null) {
				m_Anim = GetComponent<UnityEngine.Animation>();
			}
        }

		[LeafMember("PlayAnimation"), Preserve]
        public void PlayAnimation() {
			if(m_Anim) {
				m_Anim.Play();
			}
        }
		
		[LeafMember("StopAnimation"), Preserve]
        public void StopAnimation() {
			if(m_Anim) {
				m_Anim.Stop();
			}
        }
        #endregion // Leaf
		
    }
}