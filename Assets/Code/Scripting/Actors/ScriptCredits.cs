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
	
    public class ScriptCredits : ScriptActorComponent {
		
        #region Inspector
		[SerializeField]
        UnityEngine.UI.Scrollbar _scrollBar;
		
		#endregion // Inspector
		
        #region Leaf
		
        private void Awake() {
            
        }
		
        [LeafMember("Scroll"), Preserve]
        public void Scroll(float speed) {
			if(_scrollBar != null)
            {
                StartCoroutine(DoScroll(speed));
            }
        }

        IEnumerator DoScroll(float speed)
        {
            bool atEnd = false;
            while(!atEnd)
            {
                _scrollBar.value = _scrollBar.value - speed;
                if(_scrollBar.value <= 0f)
                {
                    atEnd = true;
                } 

                yield return new WaitForSeconds(0.01f);
            }
        }

        #endregion // Leaf
		
    }
}