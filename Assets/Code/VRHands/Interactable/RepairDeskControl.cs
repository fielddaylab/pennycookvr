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
		[SerializeField] GameObject RepairDesk;
		
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
		
		void Update() {

			PlayerRig handRig = Find.State<PlayerRig>();

			Vector3 currPos = Vector3.zero;

			if(LeftGrabbed || RightGrabbed) {
				
				WasGrabbed = true;
				
				if(LeftGrabbed) {
					currPos = handRig.LeftHand.Raw.transform.position;
				} else {
					currPos = handRig.RightHand.Raw.transform.position;
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
			
			PlayerRig playerRig = Find.State<PlayerRig>();
			
			if(grabber == playerRig.RightHand.Grabber) {
				RightGrabbed = true;
				LastPos = playerRig.RightHand.Raw.transform.position;
			}
			
			if(grabber == playerRig.LeftHand.Grabber) {
				LeftGrabbed = true;
				LastPos = playerRig.LeftHand.Raw.transform.position;
			}	
		}
		
		private void OnReleasePanel(Grabber grabber) {
			PlayerRig playerRig = Find.State<PlayerRig>();
			
			if(grabber == playerRig.RightHand.Grabber) {
				RightGrabbed = false;
			}
			
			if(grabber == playerRig.LeftHand.Grabber) {
				LeftGrabbed = false;
			}
		}
	}
}