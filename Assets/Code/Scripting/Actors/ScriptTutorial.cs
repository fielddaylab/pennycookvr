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
    public class ScriptTutorial : ScriptActorComponent {
		
        #region Inspector
        [SerializeField]
        private SerializedHash32 m_ToolTipText;

		#endregion // Inspector
		
        #region Leaf
		
		private void Awake() {
			
        }
		
        [LeafMember("SetTutorial"), Preserve]
        void SetTutorial(bool active)
        {
            gameObject.SetActive(active);
            
            if(active) {
                VRGame.Events.Dispatch(GameEvents.TutorialShown, EvtArgs.Create(new Data.TutorialInfo(Actor.Id, m_ToolTipText.Hash())));
            } else {
                VRGame.Events.Dispatch(GameEvents.TutorialHidden, EvtArgs.Create(new Data.TutorialInfo(Actor.Id, m_ToolTipText.Hash())));
            }
        }
        #endregion // Leaf
		
    }
}