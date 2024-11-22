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
using Pennycook.Tablet;

namespace Pennycook {
	[RequireComponent(typeof(TabletHighlightable))]
    public class ScriptTabletHighlightable : ScriptActorComponent {
		
        #region Inspector
		
		
		#endregion // Inspector
		
        #region Leaf
		private TabletHighlightable m_Highlight=null;
        private TabletInteractable Interactable=null;
        
        private void Awake() {
            m_Highlight = GetComponent<TabletHighlightable>();
            Interactable = GetComponent<TabletInteractable>();
        }
		
		public bool IsIdentified() { return m_Highlight.Identified; }
        
		[LeafMember("SetIdentified"), Preserve]
		public void SetIdentified(bool id) {
			m_Highlight.Identified = id;
		}

        [LeafMember("SetInteractable"), Preserve]
        public void SetInteractable(bool i) {
            if(Interactable != null) {
                Interactable.CanInteract = i;
            }
        }
		
		[LeafMember("IsIdentified"), Preserve]
		static public bool IsIdentified(ScriptActor actor) {
            ScriptTabletHighlightable ss = actor.GetComponent<ScriptTabletHighlightable>();
            if(ss != null) {
                return ss.IsIdentified() == true;
            }
			return false;
		}

        #endregion // Leaf

#if UNITY_EDITOR
        private void Reset() {
            Interactable = GetComponent<TabletInteractable>();
        }
#endif // UNITY_EDITOR

    }
}