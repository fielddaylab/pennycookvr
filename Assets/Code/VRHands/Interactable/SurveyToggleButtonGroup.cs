using System;
using System.Collections;
using System.Collections.Generic;
using BeauUtil;
using BeauRoutine;
using FieldDay;
using FieldDay.Audio;
using FieldDay.Components;
using FieldDay.Scripting;
using UnityEngine;


namespace Pennycook {
    public class SurveyToggleButtonGroup : BatchedComponent {
		
        #region Inspector
		public List<SurveyToggleButton> ToggleButtons = new List<SurveyToggleButton>();
		
        #endregion // Inspector
		
		private void Awake() {
			for(int i = 0; i < ToggleButtons.Count; ++i) {
				ToggleButtons[i].OnPressed.Register(DoToggle);
			}
        }

		public void DoToggle(SurveyToggleButton pressedButton) {
			if(pressedButton.IsPressed) {
				for(int i = 0; i < ToggleButtons.Count; ++i) {
					if(pressedButton != ToggleButtons[i]) {
						ToggleButtons[i].Untoggle();
					}
				}
			}
		}

		public void SetResponseText(string response, int index) {
			if(index < ToggleButtons.Count) {
				ToggleButtons[index].ResponseText.text = response;
			}
		}

		public void SetEnabled(bool enabled, int index) {
			if(index < ToggleButtons.Count) {
				ToggleButtons[index].gameObject.SetActive(enabled);
			}
		}
    }
}
