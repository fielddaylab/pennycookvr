using System;
using System.Collections;
using System.Collections.Generic;
using BeauUtil;
using BeauRoutine;
using FieldDay;
using FieldDay.Components;
using FieldDay.Scripting;
using UnityEngine;


namespace Pennycook {
    public class Button : BatchedComponent {
		
        #region Inspector
        [Required]
		public TriggerListener Detector;
		
		[Required]
		public GameObject Mesh;
		
        public bool Locked = false;
        
        public bool Toggleable = false;
		
		public bool UseLocalSpace = false;
		
		public Vector3 ButtonShift;
		
        #endregion // Inspector
		
		[NonSerialized] public bool WasPressed = false;

        [NonSerialized] public bool IsPressed = false;
		
		private Rigidbody CachedRB;
		
		private Transform CachedTransform;
		
		private bool IsIn = false;

        public readonly CastableEvent<Button> OnPressed = new CastableEvent<Button>();
		
		private void Awake() {

			Detector.onTriggerEnter.AddListener(ButtonTrigger);
			Detector.onTriggerExit.AddListener(ButtonTriggerExit);
			
			CachedRB = GetComponent<Rigidbody>();
			
			CachedTransform = Mesh.transform;
        }

		public void Untoggle() {
			IsPressed = false;
			CachedTransform.position = CachedTransform.TransformPoint(-ButtonShift);
		}
		
		public void ButtonTriggerExit(Collider c) {
			IsIn = false;
			if(!Toggleable) {
				Routine.Start(this, ShiftBack(c));
			}
		}

		public void ButtonTrigger(Collider c) {

			if(!IsIn) {
				if(!Locked) {
					if(Toggleable) {					
	
						if(CachedRB != null) {
							CachedRB.detectCollisions = false;
						}
						
						if(!WasPressed) {
							WasPressed = true;
						}
						
						IsPressed = !IsPressed;

						//Sfx.OneShot("button-click", transform.position);
						
						if(!IsPressed) {
							CachedTransform.Translate(ButtonShift);
						} else {
							CachedTransform.Translate(-ButtonShift);
						}
						
						Routine.Start(this, TurnBackOn(c));
						
					} else {
						
						if(CachedRB != null) {
							CachedRB.detectCollisions = false;
						}
						
						if(!WasPressed) {
							//this should only happen if we're on the first level...
							/*if(gameObject.name == "ArgoFaceButton") {
								SceneLoader sceneInfo = Find.State<SceneLoader>();
								if(sceneInfo.GetCurrentSceneIndex() == 0) {
									ScriptPlugin.ForceKill = true;
									StartCoroutine(ArgoWasPressed(1f));
								}
							}*/

							WasPressed = true;
						}

						//Sfx.OneShot("button-click", transform.position);

						if (UseLocalSpace)
						{
							 CachedTransform.Translate(ButtonShift, Space.Self);
						}
						else
						{
							CachedTransform.Translate(-ButtonShift);
						}
						
					}
					
					//haptics...
					//todo - optimize
					/*VRInputState data = Find.State<VRInputState>();
					if(c.gameObject.name.StartsWith("Left")) {
						data.LeftHand.HapticImpulse = 0.25f;
					} else if(c.gameObject.name.StartsWith("Right")) {
						data.RightHand.HapticImpulse = 0.25f;
					}*/

					OnPressed.Invoke(this);

					IsIn = true;
				}
			}
		}
		
		IEnumerator ShiftBack(Collider c) {
			yield return 0.2f;
			if(UseLocalSpace)
			{
				CachedTransform.Translate(-ButtonShift, Space.Self);
			}
			else
			{
				CachedTransform.Translate(ButtonShift);
			}
			
			Routine.Start(this, TurnBackOn(c));
		}
		
		IEnumerator TurnBackOn(Collider c) {
			yield return 0.2f;
			if(CachedRB != null) {
				CachedRB.detectCollisions = true;
			}
		}
    }
}
