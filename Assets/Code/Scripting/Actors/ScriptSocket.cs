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

        static public List<ScriptSocket> m_HighlightSockets = new List<ScriptSocket>();
        
        [LeafMember("ClearHighlightSockets"), Preserve]
        static public void LeafClearHighlightSockets() {
            m_HighlightSockets.Clear();
            //Debug.Log("Clearing sockets");
        }

        [LeafMember("TurnOnSockets"), Preserve]
        static public void LeafTurnOnSockets(StringHash32 SocketClassName, bool On) {
            for(int i = 0; i < m_HighlightSockets.Count; ++i) {
                if(m_HighlightSockets[i].Actor != null && m_HighlightSockets[i].Actor.ClassName == SocketClassName) {
                    m_HighlightSockets[i].TurnOnHighlight(On);
                }
            }
        }

        private void Awake() {
            m_Socket = GetComponent<ObjectSocket>();
            if(m_Socket.HighlightPair != null) {
                m_HighlightSockets.Add(this);
                //Debug.Log("Adding socket");
            }
        }

        public void TurnOnHighlight(bool On) {
            if(m_Socket.HighlightPair != null) {
                m_Socket.HighlightPair.SetActive(On);
            }
        }
		
		public bool IsSocketed() { return m_Socket.Current != null; }
        public bool IsSocketedBy(Socketable s) { return m_Socket.Current == s; }

        [LeafMember("SetLocked"), Preserve]
        public void SetLocked(bool lockParam) {
			m_Socket.Locked = lockParam;
        }
		
		[LeafMember("ReleaseCurrentSocket"), Preserve]
		public bool ReleaseCurrentSocket(bool highlight=false) {
            if(m_Socket != null) {
				SocketUtility.ReleaseCurrent(m_Socket, false, highlight);
				return true;
            }
			
			return false;
		}
		
		[LeafMember("IsSocketedBy"), Preserve]
		static public bool IsSocketedBy(ScriptActor actor, ScriptActor socket) {
            ScriptSocket ss = actor.GetComponent<ScriptSocket>();
            Socketable s = socket.GetComponent<Socketable>();
            if(ss != null && s != null) {
                return ss.IsSocketedBy(s);
            }
			return false;
		}
		
		[LeafMember("SocketObjectTo"), Preserve]
		static public bool SocketObjectTo(ScriptActor actor, ScriptActor socket) {
            ScriptSocket ss = socket.GetComponent<ScriptSocket>();
            Socketable s = actor.GetComponent<Socketable>();
            if(ss != null && s != null) {
                return SocketUtility.TryAddToSocket(s, ss.GetComponent<ObjectSocket>(), false);
            }
			return false;
		}
		

		
        #endregion // Leaf
		
    }
}