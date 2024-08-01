using FieldDay.SharedState;
using UnityEngine;

namespace FieldDay.XR {
    public sealed class XRUserRig : SharedStateComponent {
        public Transform Root;

        public XRTrackedTransform Head;
        public XRTrackedTransform LeftHand;
        public XRTrackedTransform RightHand;
    }
}