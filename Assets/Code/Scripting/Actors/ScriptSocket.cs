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
	[RequireComponent(typeof(ObjectSocket))]
    public class ScriptSocket : ScriptActorComponent {
		
        #region Inspector
		
		
		#endregion // Inspector
		
        #region Leaf
		private ObjectSocket m_Socket=null;
        
        private void Awake() {
            m_Socket = GetComponent<ObjectSocket>();
        }
		
		public bool IsSocketed() { return m_Socket.Current != null; }
        public bool IsSocketedBy(Socketable s) { return m_Socket.Current == s; }

        [LeafMember("SetLocked"), Preserve]
        public void SetLocked(bool lockParam) {
			m_Socket.Locked = lockParam;
        }

		[LeafMember("IsSocketedBy"), Preserve]
		static public bool IsSocketedBy(ScriptActor actor, ScriptActor socket) {
            ScriptSocket ss = actor.GetComponent<ScriptSocket>();
            Socketable s = socket.GetComponent<Socketable>();
            if(ss != null && s != null) {
                return !ss.IsSocketedBy(s);
            }
			return false;
		}
		
        [LeafMember("NotIsSocketedBy"), Preserve]
		static public bool NotIsSocketedBy(ScriptActor actor, ScriptActor socket) {
            ScriptSocket ss = actor.GetComponent<ScriptSocket>();
            Socketable s = socket.GetComponent<Socketable>();
            if(ss != null && s != null) {
                return ss.IsSocketedBy(s);
            }
			return false;
		}

        #endregion // Leaf
		
    }
}