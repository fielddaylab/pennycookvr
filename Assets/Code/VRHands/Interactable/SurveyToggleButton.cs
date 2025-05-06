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
    public class SurveyToggleButton : BatchedComponent {
		
        #region Inspector
        [Required]
		public TriggerListener Detector;
		
		[SerializeField]
		Texture2D FilledGraphic;
		
		[SerializeField]
		Texture2D UnfilledGraphic;
		
        #endregion // Inspector
		
		[NonSerialized] public bool WasPressed = false;

        [NonSerialized] public bool IsPressed = false;
		
		//private Rigidbody CachedRB;
		private MeshRenderer CachedMR;
		
        public readonly CastableEvent<SurveyToggleButton> OnPressed = new CastableEvent<SurveyToggleButton>();
		
		[AudioEventRef]
		public StringHash32 ButtonSound;
		
		private void Awake() {

			Detector.onTriggerEnter.AddListener(ButtonTrigger);

			CachedMR = GetComponent<MeshRenderer>();

        }

		public void Untoggle() {
			IsPressed = false;
			if(CachedMR != null) {
				CachedMR.material.mainTexture = UnfilledGraphic;
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
			
			if(!IsPressed) {
				//switch to unfilled graphic
				CachedMR.material.mainTexture = UnfilledGraphic;
			} else {
				//switch to filled graphic
				CachedMR.material.mainTexture = FilledGraphic;
			}

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
