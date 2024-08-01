using System;
using BeauUtil;
using FieldDay;
using FieldDay.HID.XR;
using FieldDay.SharedState;
using FieldDay.VRHands;

namespace Pennycook.Tablet {
    public class TabletControlState : SharedStateComponent, IRegistrationCallbacks {
        public Grabbable Grabbable;

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
        }

        private void OnGrabReleased(Grabber grabber) {
            if (grabber.Chirality != XRHandIndex.Any) {
                GrippedHandMask.Unset((int) grabber.Chirality);
            }
        }
    }
}