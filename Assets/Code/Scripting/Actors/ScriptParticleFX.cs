using System;
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

namespace Pennycook.Scripting {
    public class ScriptParticleFX : ScriptActorComponent {
		
        #region Inspector
		[SerializeField, Required] private ParticleSystem m_PS = null;
        #endregion // Inspector
		
        #region ILeafActor

        #endregion // ILeafActor

        bool m_IsDestination = false;

        #region Leaf

        [LeafMember("PlayFX"), Preserve]
        public void PlayFX() {
			if(m_PS != null) {
				m_PS.Play();
			}
        }

        [LeafMember("StopFX"), Preserve]
        public void StopFX() {
            if(m_PS != null) {
				m_PS.Stop();
			}
        }
		
		[LeafMember("IncreaseParticles"), Preserve]
		public void IncreaseParticles(int numParticles, float rate) {
			if(m_PS != null) {
				ParticleSystem.MainModule m = m_PS.main;
				m.maxParticles = numParticles;
				ParticleSystem.EmissionModule e = m_PS.emission;
				e.rateOverTime = rate;
			}
		}


        #endregion // Leaf

        #region Unity Events

#if UNITY_EDITOR

#endif // UNITY_EDITOR

        #endregion // Unity Events
    }
}