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
    public class ScriptDoor : ScriptActorComponent {
		
        #region Inspector
		
		
		#endregion // Inspector
		
        #region Leaf
		private Animator m_Anim=null;
        
        private void Awake() {
            m_Anim = GetComponent<Animator>();
        }
		
		public bool IsDoorOpen() { return m_Anim.GetBool("open"); }
		
        [LeafMember("Open"), Preserve]
        public void Open() {
			m_Anim.SetBool("open", true);
        }
		
		[LeafMember("Close"), Preserve]
        public void Close() {
			m_Anim.SetBool("open", false);
        }
		
		[LeafMember("IsOpen"), Preserve]
		static public bool IsOpen(ScriptActor actor) {
			ScriptDoor sd = actor.GetComponent<ScriptDoor>();
			if(sd != null) {
				return sd.IsDoorOpen();
			}
			return false;
		}

        #endregion // Leaf
		
    }
}