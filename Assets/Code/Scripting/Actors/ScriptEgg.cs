using System;
using System.Collections;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Variants;
using BeauRoutine;
using FieldDay;
using FieldDay.Components;
using FieldDay.Scenes;
using FieldDay.Scripting;
using FieldDay.Sockets;
using Leaf.Runtime;
using UnityEngine;
using UnityEngine.Scripting;


namespace Pennycook {
	
    public class ScriptEgg : ScriptActorComponent {
		
        #region Inspector
		[SerializeField]
		private Animator m_Anim=null;
		
		[SerializeField]
		private GameObject m_ShellParent=null;

		[SerializeField]
		private Animator m_ChickAnim=null;

		[SerializeField]
		Cheeper _newbornCheeper;
		
		#endregion // Inspector
		
        #region Leaf
		
        private bool _chickStarting = false;
		
        private void Awake() {
            
        }
		
        [LeafMember("Hatch"), Preserve]
        public void Hatch() {
			if(!_chickStarting) {
				if (m_Anim != null)
				{
					m_Anim.SetTrigger("shake");
				}

				_newbornCheeper.SetRate(30);
				_newbornCheeper.SetState(Cheeper.CheepState.Muffled);
				_newbornCheeper.SetFade(0, 0.8f, 3f);

				_chickStarting = false;

				Routine.Start(this, FinishChickSequence(8f));
			}
        }

		IEnumerator FinishChickSequence(float waitTime)
		{
			yield return new WaitForSeconds(waitTime);

			// Breaking egg
			m_ShellParent.SetActive(true);
			m_ChickAnim.gameObject.SetActive(true);
	
			if(m_ShellParent != null)
			{
				m_ShellParent.GetComponent<Animator>().SetTrigger("break");
				yield return new WaitForSeconds(0.1f);
				m_ChickAnim.SetTrigger("break");
			}
			
			yield return new WaitForSeconds(8f);
			waitTime -= 8f;

			// Emerging Chick
			_newbornCheeper.SetRate(120);
			_newbornCheeper.SetState(Cheeper.CheepState.Open);
			_newbornCheeper.SetFade(0.8f, 1, 3f);
			
			m_Anim.SetTrigger("stop");
			m_Anim.gameObject.SetActive(false);

			yield return new WaitForSeconds(5f);
			waitTime -= 5f;

			//m_ChickAnim.SetTrigger("stop");

			// Hearts above emerging chick

			//_hatchHeartParticles.Play();

			// Animation completed
		}
        #endregion // Leaf
		
    }
}