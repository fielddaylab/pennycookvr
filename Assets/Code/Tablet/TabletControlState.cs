using System;
using BeauUtil;
using FieldDay;
using FieldDay.HID.XR;
using FieldDay.Scripting;
using FieldDay.SharedState;
using FieldDay.VRHands;
using UnityEngine;

namespace Pennycook.Tablet {
    public class TabletControlState : SharedStateComponent, IRegistrationCallbacks {
        public Grabbable Grabbable;
        public Transform AudioLocation;

        [NonSerialized] public BitSet32 GrippedHandMask;

        void IRegistrationCallbacks.OnDeregister() {
            Grabbable.OnGrabbed.Deregister(OnGrabbed);
            Grabbable.OnReleased.Deregister(OnGrabReleased);
        }

        void IRegistrationCallbacks.OnRegister() {
            Grabbable.OnGrabbed.Register(OnGrabbed);
            Grabbable.OnReleased.Register(OnGrabReleased);
        }

        private void OnGrabbed(Grabber grabber) {
            StringHash32 nodeName = GrabUtility.ResolveSnapNodeName(grabber);
            switch (grabber.Chirality) {
                case XRHandIndex.Left: {
                    if (nodeName == "LeftHandle") {
                        GrippedHandMask.Set((int) XRHandIndex.Left);
                    }
                    break;
                }
                case XRHandIndex.Right: {
                    if (nodeName == "RightHandle") {
                        GrippedHandMask.Set((int) XRHandIndex.Right);
                    }
                    break;
                }
            }

            if (GrippedHandMask.Count == 1) {
                ScriptUtility.Trigger(GameTriggers.LiftedTablet);
            }
        }

        private void OnGrabReleased(Grabber grabber) {
            if (grabber.Chirality != XRHandIndex.Any) {
                GrippedHandMask.Unset((int) grabber.Chirality);

                if (GrippedHandMask.IsEmpty) {
                    ScriptUtility.Trigger(GameTriggers.DroppedTablet);
                }
            }
        }
    }

    static public partial class TabletUtility {
        static public void PlayHaptics(float amp, float duration) {
            TabletControlState ctrl = Find.State<TabletControlState>();
            if (ctrl.GrippedHandMask.IsSet((int) XRHandIndex.Left)) {
                PlayerHaptics.Play(XRHandIndex.Left, amp, duration);
            }
            if (ctrl.GrippedHandMask.IsSet((int) XRHandIndex.Right)) {
                PlayerHaptics.Play(XRHandIndex.Right, amp, duration);
            }
        }
    }
}