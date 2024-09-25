using FieldDay;
using FieldDay.Components;
using FieldDay.HID.XR;
using FieldDay.Physics;
using FieldDay.VRHands;
using FieldDay.XR;
using UnityEngine;

namespace Pennycook {
    public class PlayerHandRig : BatchedComponent, IRegistrationCallbacks {
        public XRHandIndex Hand;
        public XRTrackedTransform Raw;

        [Header("Visuals")]
        public GrabPosedHand Pose;

        [Header("Physics")]
        public Grabber Grabber;
        public RBInterpolator Interpolator;
        public Collider Finger;

        void IRegistrationCallbacks.OnDeregister() {
        }

        void IRegistrationCallbacks.OnRegister() {
            Grabber.OnGrab.Register(() => {
                Finger.enabled = false;
            });
            Grabber.OnRelease.Register(() => {
                Finger.enabled = true;
            });
        }
    }

    static public class PlayerHaptics {
        static public void Play(XRHandIndex hand, float amp, float duration) {
            XRInputUtility.RequestHaptics(hand, amp, duration);
        }
    }
}