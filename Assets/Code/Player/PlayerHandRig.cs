using FieldDay.Components;
using FieldDay.HID.XR;
using FieldDay.Physics;
using FieldDay.VRHands;
using FieldDay.XR;
using UnityEngine;

namespace Pennycook {
    public class PlayerHandRig : BatchedComponent {
        public XRHandIndex Hand;
        public XRTrackedTransform Raw;

        [Header("Visuals")]
        public GrabPosedHand Pose;

        [Header("Physics")]
        public Grabber Grabber;
        public RBInterpolator Interpolator;
    }
}