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
    [RequireComponent(typeof(LoadSurvey))]
    public class ScriptSurvey : ScriptActorComponent {
		
        #region Inspector
		private LoadSurvey m_Survey = null;
        
		#endregion // Inspector
		
        #region Leaf
		
		private void Awake() {
			if(m_Survey == null) {
				m_Survey = GetComponent<LoadSurvey>();
			}
        }
		
        [LeafMember("ShowSurvey"), Preserve]
        void ShowSurvey(int surveyIndex, int stopIndex)
        {
            m_Survey.SetSurveyIndex(surveyIndex, stopIndex);
            m_Survey.LoadLikertQuestions(surveyIndex == 0);
        }
        #endregion // Leaf
		
    }
}