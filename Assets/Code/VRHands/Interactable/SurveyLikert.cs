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
    public class SurveyLikert : BatchedComponent {
		
        #region Inspector
		[SerializeField]
		TMPro.TextMeshPro QuestionText;


        #endregion // Inspector
		
		[NonSerialized]
		public bool IsCustom = false;
		
		SurveyToggleButtonGroup ToggleGroup;

		private void Awake() {
			ToggleGroup = GetComponent<SurveyToggleButtonGroup>();
        }

		public TMPro.TextMeshPro GetQuestion() {
			return QuestionText;
		}

		public void SetCustomResponse(string response, int index) {
			if(ToggleGroup) {
				ToggleGroup.SetResponseText(response, index);
			}
		}

		public void SetEnabled(bool enabled, int index) {
			if(ToggleGroup) {
				ToggleGroup.SetEnabled(enabled, index);
			}
		}
    }
}
