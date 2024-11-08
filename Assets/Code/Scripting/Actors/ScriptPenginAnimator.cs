using System;
using System.Collections;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Variants;
using FieldDay;
using FieldDay.Components;
using FieldDay.Scenes;
using FieldDay.Scripting;
using FieldDay.Sockets;
using Leaf.Runtime;
using UnityEngine;
using UnityEngine.Scripting;


namespace Pennycook {
	[RequireComponent(typeof(Animator))]
    public class ScriptPenguinAnimator : ScriptActorComponent {
		
        #region Inspector
		
		
		#endregion // Inspector
		
        #region Leaf
		private Animator m_Anim=null;
        
        private void Awake() {
            m_Anim = GetComponent<Animator>();
        }
		
        [LeafMember("Regurgitate"), Preserve]
        public void Regurgitate() {
			m_Anim.SetTrigger("Regurgitate");
        }
		
        #endregion // Leaf
		
    }
}