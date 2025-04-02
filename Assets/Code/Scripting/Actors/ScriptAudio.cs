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
    [RequireComponent(typeof(UnityEngine.AudioSource))]
    public class ScriptAudio: ScriptActorComponent {
		
        #region Inspector
		private AudioSource m_Audio = null;
        
		[SerializeField]
		AudioClip m_Indoor;

		[SerializeField]
		AudioClip m_Outdoor;
		#endregion // Inspector
		
        #region Leaf
		
		private void Awake() {
			if(m_Audio == null) {
				m_Audio = GetComponent<UnityEngine.AudioSource>();
			}
        }

		[LeafMember("PlayAudio"), Preserve]
        public void PlayAudio() {
			if(m_Audio) {
				m_Audio.Play();
			}
        }
		
		[LeafMember("StopAudio"), Preserve]
        public void StopAudio() {
			if(m_Audio) {
				m_Audio.Stop();
			}
        }

		[LeafMember("PlayIndoor"), Preserve]
        public void PlayIndoor() {
			if(m_Audio) {
				m_Audio.clip = m_Indoor;
				m_Audio.Play();
			}
        }
		
		[LeafMember("PlayOutdoor"), Preserve]
        public void PlayOutdoor() {
			if(m_Audio) {
				m_Audio.clip = m_Outdoor;
				m_Audio.Play();
			}
        }

		[LeafMember("SetVolume"), Preserve]
        public void SetVolume(float v) {
			if(m_Audio) {
				m_Audio.volume = v;
			}
        }
		
        #endregion // Leaf
		
    }
}