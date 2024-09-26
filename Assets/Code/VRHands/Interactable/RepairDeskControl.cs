using System;
using System.Collections;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Components;
using FieldDay.SharedState;
using FieldDay.Systems;
using FieldDay.VRHands;
using UnityEngine;
using UnityEngine.XR;

namespace Pennycook {
	public class RepairDeskControl : BatchedComponent
	{
		/*[SerializeField] GameObject RepairDesk;
		
		[SerializeField] float MaxMove = 0.8975f;
		[SerializeField] float MinMove = 0.615f;
		
		private bool RightGrabbed = false;
		private bool LeftGrabbed = false;
		
		private Grabbable Handle;
		
		[NonSerialized] public bool WasGrabbed = false;
		
		private Vector3 LastPos = Vector3.zero;
		
		// Start is called before the first frame update
        private void Awake() {
			Handle = GetComponent<Grabbable>();
			if(Handle != null) {
				Handle.OnGrabbed.Register(OnGrabPanel);
				Handle.OnReleased.Register(OnReleasePanel);
			}
        }
		
		void Start() {

		}
		
		
		public void MoveDesk()
		{ 
		
		}
		
		void Update() {

			PlayerHandRig handRig = Find.State<PlayerHandRig>();

			Vector3 currPos = Vector3.zero;

			if(LeftGrabbed || RightGrabbed) {
				
				WasGrabbed = true;
				
				if(LeftGrabbed) {
					currPos = handRig.LeftHand.Visual.position;
				} else {
					currPos = handRig.RightHand.Visual.position;
				}
				

				float dir = 1f;
				if(LastPos.y - currPos.y > 0f) {
					dir = -1f;
				}

				if(RepairDesk != null) {
					Vector3 vTrans = Vector3.up * dir * Vector3.Distance(LastPos, currPos);
					if(RepairDesk.transform.position.y + vTrans.y < MaxMove && RepairDesk.transform.position.y + vTrans.y > MinMove)
					{
						RepairDesk.transform.Translate(vTrans, Space.World);
					}
				}
			
				LastPos = currPos;
			} 
		}
		
		private void OnGrabPanel(Grabber grabber) {
			
			PlayerHandRig handRig = Find.State<PlayerHandRig>();
			
			if(grabber == handRig.RightHand.Physics) {
				RightGrabbed = true;
				LastPos = handRig.RightHand.Visual.position;
				WSAnalytics w = Find.State<WSAnalytics>();
				if(w != null) {
                	w.LogGrabWorkBenchHandle(false, RepairDesk.transform.position.y);
				}
			}
			
			if(grabber == handRig.LeftHand.Physics) {
				LeftGrabbed = true;
				LastPos = handRig.LeftHand.Visual.position;
				WSAnalytics w = Find.State<WSAnalytics>();
				if(w != null) {
                	w.LogGrabWorkBenchHandle(true, RepairDesk.transform.position.y);
				}
			}	
		}
		
		private void OnReleasePanel(Grabber grabber) {
			PlayerHandRig handRig = Find.State<PlayerHandRig>();
			
			if(grabber == handRig.RightHand.Physics) {
				RightGrabbed = false;
				WSAnalytics w = Find.State<WSAnalytics>();
				if(w != null) {
                	w.LogReleaseWorkBenchHandle(false, RepairDesk.transform.position.y);
				}
			}
			
			if(grabber == handRig.LeftHand.Physics) {
				LeftGrabbed = false;
				WSAnalytics w = Find.State<WSAnalytics>();
				if(w != null) {
                	w.LogReleaseWorkBenchHandle(true, RepairDesk.transform.position.y);
				}
			}
		}*/
	}
}