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
    public class SurveyNextButton : BatchedComponent {
		
        #region Inspector
        [Required]
		public TriggerListener Detector;
		
		[SerializeField]
		Color EnabledColor;
		
		[SerializeField]
		Color TextEnabledColor;

		[SerializeField]
		Color DisabledColor;
		
		[SerializeField]
		Color TextDisabledColor;
        #endregion // Inspector
		
		[NonSerialized] public bool WasPressed = false;

        [NonSerialized] public bool IsPressed = false;
		
		private MeshRenderer CachedMR;
		
        public readonly CastableEvent<SurveyNextButton> OnPressed = new CastableEvent<SurveyNextButton>();
		
		[AudioEventRef]
		public StringHash32 ButtonSound;
		
		[NonSerialized] public TMPro.TextMeshPro NextText;

		private void Awake() {

			Detector.onTriggerEnter.AddListener(ButtonTrigger);

			CachedMR = GetComponent<MeshRenderer>();

			if(transform.childCount > 0) {
				NextText = transform.GetChild(0).GetComponent<TMPro.TextMeshPro>();
			}
        }

		public void EnableNextButton(bool enabled) {
			if(!enabled) {
				//switch to unfilled graphic
				CachedMR.material.color = DisabledColor;
				NextText.color = TextDisabledColor;
			} else {
				//switch to filled graphic
				CachedMR.material.color = EnabledColor;
				NextText.color = TextEnabledColor;
			}
		}
		
		public void ButtonTrigger(Collider c) {

			/*if(CachedRB != null) {
				CachedRB.detectCollisions = false;
			}*/
			
			if(!WasPressed) {
				WasPressed = true;
			}
			
			IsPressed = !IsPressed;

			//Sfx.OneShot("button-click", transform.position);

			Sfx.Play(ButtonSound, transform);
			
			//haptics...
			//todo - optimize
			/*VRInputState data = Find.State<VRInputState>();
			if(c.gameObject.name.StartsWith("Left")) {
				data.LeftHand.HapticImpulse = 0.25f;
			} else if(c.gameObject.name.StartsWith("Right")) {
				data.RightHand.HapticImpulse = 0.25f;
			}*/
			OnPressed.Invoke(this);
		}
    }
}
