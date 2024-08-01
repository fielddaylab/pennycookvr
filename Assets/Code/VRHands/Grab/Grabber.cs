using System;
using BeauUtil;
using FieldDay.Components;
using FieldDay.HID.XR;
using UnityEngine;
using FieldDay.Physics;

namespace FieldDay.VRHands {
    [DisallowMultipleComponent]
    public class Grabber : BatchedComponent {
        #region Inspector

        public Transform GripCenter;
        public float GripRadius = 1;
        public LayerMask GripMask;
        public XRHandIndex Chirality;

        [Header("Configuration")]
        public float MinGripForce = 20;
        public float MaxGripForce = 100;
        [Range(0, 1)] public float HeavyGripForceMultiplier = 0.7f;
        public float ReleaseThrowForce = 0.4f;
        public SerializedFixedJoint JointConfig = SerializedFixedJoint.Default;

        #endregion // Inspector

        [NonSerialized] public Transform CachedTransform;
        [NonSerialized] public Rigidbody CachedRB;

        [NonSerialized] public FixedJoint Joint;
        [NonSerialized] public GrabberState State = GrabberState.Empty;
        [NonSerialized] public Grabbable HeldObject;
        [NonSerialized] public int HeldObjectSnapNodeIndex = -1;
        [NonSerialized] public long HoldStartTime;

        #region Events

        public GrabberPredicate CanGrab = CanGrabPassThrough;
        public readonly CastableEvent<Grabbable> OnGrab = new CastableEvent<Grabbable>();
        public readonly ActionEvent OnGrabFailed = new ActionEvent();
        public readonly CastableEvent<Grabbable> OnRelease = new CastableEvent<Grabbable>();

        #endregion // Events

        private void Awake() {
            this.CacheComponent(ref CachedTransform);
            this.CacheComponent(ref CachedRB);

            if (!GripCenter) {
                GripCenter = CachedTransform;
            }
        }

        static private readonly GrabberPredicate CanGrabPassThrough = (a, b) => true;
    }

    public delegate bool GrabberPredicate(Grabber grabber, Grabbable grabbable);

    public enum GrabberState {
        Empty,
        AttemptGrab,
        Holding,
        AttemptRelease,
        AttemptReleaseSocketOnly
    }
}