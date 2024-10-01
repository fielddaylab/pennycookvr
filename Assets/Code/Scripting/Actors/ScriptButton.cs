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
    [RequireComponent(typeof(Button))]
    public class ScriptButton : ScriptActorComponent {
		
        #region Inspector
		private Button m_Button = null;
        
		#endregion // Inspector
		
        #region Leaf
		
		private void Awake() {
			if(m_Button == null) {
				m_Button = GetComponent<Button>();
			}
        }
		
		public bool WasButtonPressed() { return ((m_Button != null) && m_Button.WasPressed); }
		
        [LeafMember("SetButtonLocked"), Preserve]
        public void SetButtonLocked(bool lockParam) {
			m_Button.Locked = lockParam;
        }
		
		[LeafMember("SetButtonPressed"), Preserve]
        public void SetButtonPressed(bool pressed) {
			m_Button.WasPressed = pressed;
        }

		[LeafMember("ButtonNotPressed"), Preserve]
		static bool ButtonNotPressed(ScriptActor actor) {
			
            ScriptButton sb = actor.GetComponent<ScriptButton>();
            if(sb != null) {
                return !sb.WasButtonPressed();
            }
        
			return false;
		}
		
		[LeafMember("ButtonPressed"), Preserve]
		static bool ButtonPressed(ScriptActor actor) {
			
            ScriptButton sb = actor.GetComponent<ScriptButton>();
            if(sb != null) {
                return sb.WasButtonPressed();
            }
        
			return false;
		}
		
        #endregion // Leaf
		
    }
}