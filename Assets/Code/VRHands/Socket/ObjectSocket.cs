using System;
using BeauUtil;
using FieldDay.Components;
using FieldDay.Physics;
using UnityEngine;

namespace FieldDay.Sockets {
    [DefaultExecutionOrder(1)]
    public class ObjectSocket : BatchedComponent {
        #region Inspector

        public bool Locked = false;
        public Socketable Current;

        [Header("Configuration")]
        public SocketMode Mode = SocketMode.Reparent;
        [ShowIfField("IsFixedJointMode")] public SerializedFixedJoint JointConfig = SerializedFixedJoint.Default;
        [Space]
        public Vector3 ReleaseForce;

        [Header("Components")]
        [Required] public Transform Location;
        [Required] public TriggerListener Detector;

        #endregion // Inspector

        [NonSerialized] public FixedJoint CurrentJoint;
        [NonSerialized] public int HighlightCount;

        #region Events

        public SocketPredicate CanAdd = CanAddPassThrough;
        public readonly CastableEvent<int> OnHighlightCountUpdated = new CastableEvent<int>();
        public readonly CastableEvent<Socketable> OnAdded = new CastableEvent<Socketable>();
        public readonly CastableEvent<Socketable> OnRemoved = new CastableEvent<Socketable>();

        #endregion // Events

        static private readonly SocketPredicate CanAddPassThrough = (a, b) => true;

        #region Unity Events

        private void Awake() {
            if (!Location) {
                Location = transform;
            }

            Detector.onTriggerEnter.AddListener(OnDetectorEntered);
            Detector.onTriggerExit.AddListener(OnDetectorExited);
        }

        private void Start() {
            if (Current != null && Current.CurrentSocket == null) {
                SocketUtility.TryAddToSocket(Current, this, true);
            } else {
                Current = null;
            }
        }

        private void OnDetectorEntered(Collider collider) {
            Socketable socketable = collider.GetComponentInParent<Socketable>();
            if (socketable) {
                socketable.PotentialSockets.Add(this);
            }
        }

        private void OnDetectorExited(Collider collider) {
            if (!collider) {
                return;
            }

            Socketable socketable = collider.GetComponentInParent<Socketable>();
            if (socketable) {
                socketable.PotentialSockets.Remove(this);
            }
        }

        #endregion // Unity Events

#if UNITY_EDITOR
        private bool IsFixedJointMode() {
            return Mode == SocketMode.FixedJoint;
        }
#endif // UNITY_EDITOR
    }

    public delegate bool SocketPredicate(ObjectSocket socket, Socketable socketable);

    public enum SocketMode {
        Reparent,
        FixedJoint,
        Custom
    }
}