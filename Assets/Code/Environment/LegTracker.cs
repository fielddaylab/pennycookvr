using System.Collections;
using System.Collections.Generic;
using BeauUtil;
using FieldDay;
using FieldDay.Components;
using FieldDay.Sockets;
using FieldDay.Scripting;
using FieldDay.VRHands;
using UnityEngine;
using Pennycook.Tablet;

namespace Pennycook {

    [RequireComponent(typeof(Grabbable))]
    public class LegTracker : BatchedComponent , IRegistrationCallbacks {

        static public readonly StringHash32 RemovedTracker = "TrackerRemoved";

        #region Inspector
        public GameObject TrackedPenguin;
        #endregion

        private Grabbable GrabComponent;

        void IRegistrationCallbacks.OnRegister() {
            this.CacheComponent(ref GrabComponent);
            GrabComponent.OnGrabbed.Register(OnGrabbed);
            GrabComponent.OnReleased.Register(OnGrabReleased);
        }
        void IRegistrationCallbacks.OnDeregister() {
            GrabComponent.OnGrabbed.Deregister(OnGrabbed);
            GrabComponent.OnReleased.Deregister(OnGrabReleased);
        }

        private void OnGrabbed(Grabber grabber, int snapIndex) {
            if(TrackedPenguin != null) {
                if(TrackedPenguin.name == "Fuzz") {
                    var actor = ScriptUtility.Actor(this);
                    if (actor != null) {
                        using(var table = TempVarTable.Alloc()) {
                            table.ActorInfo(actor);
                            ScriptUtility.Trigger(LegTracker.RemovedTracker, table);
                        }
                    }
                }
            }
        }

        private void OnGrabReleased(Grabber grabber, int snapIndex) {

        }
		
		private void Awake() {
			Grabbable g = GetComponent<Grabbable>();
			if(g != null) {
				g.DefaultRBKinematic = false;
			}
		}
    }
}
