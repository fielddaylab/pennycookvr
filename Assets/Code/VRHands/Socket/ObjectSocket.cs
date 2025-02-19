using System;
using BeauUtil;
using FieldDay.Components;
using FieldDay.Physics;
using FieldDay.Scenes;
using UnityEngine;

namespace FieldDay.Sockets {
    [DefaultExecutionOrder(1)]
    public class ObjectSocket : BatchedComponent, ISceneLoadHandler {
        #region Inspector

        public bool Locked = false;
        public Socketable Current;
		public SocketFlags AllowedSockets = SocketFlags.Nothing;
		
        [Header("Configuration")]
        public SocketMode Mode = SocketMode.Reparent;
        [ShowIfField("IsFixedJointMode")] public SerializedFixedJoint JointConfig = SerializedFixedJoint.Default;
        [Space]
        public Vector3 ReleaseForce;
		

        [Header("Components")]
        [Required] public Transform Location;
        [Required] public TriggerListener Detector;

        public GameObject HighlightPair;

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

            if (Current) {
                Current.OriginalSocket = this;
            }
			
			if (Current) {
                SocketUtility.TryAddToSocket(Current, this, true);
            }
			
            Detector.onTriggerEnter.AddListener(OnDetectorEntered);
            Detector.onTriggerExit.AddListener(OnDetectorExited);
        }

		public bool IsSocketAllowed(SocketFlags Flags) {
            return ((AllowedSockets & Flags) != 0);
        }
		
        void ISceneLoadHandler.OnSceneLoad(SceneBinding inScene, object inContext) {
            if (Current != null && Current.CurrentSocket == null) {
                SocketUtility.TryAddToSocket(Current, this, true);
            } else {
                Current = null;
            }
        }

        private void OnDetectorEntered(Collider collider) {
            Socketable socketable = collider.GetComponentInParent<Socketable>();
            if (socketable) {
				if(socketable.SocketType == AllowedSockets) {
					FieldDay.VRHands.Grabbable grabbable = collider.GetComponentInParent<FieldDay.VRHands.Grabbable>();
					if(grabbable != null) {
						if(grabbable.CurrentGrabbers[0] != null && grabbable.CurrentGrabbers[0].State == FieldDay.VRHands.GrabberState.Holding) {
							Pennycook.PlayerHaptics.Play(grabbable.CurrentGrabbers[0].Chirality, 0.3f, 0.5f);
						}
						
						if(grabbable.CurrentGrabbers[1] != null && grabbable.CurrentGrabbers[1].State == FieldDay.VRHands.GrabberState.Holding) {
							Pennycook.PlayerHaptics.Play(grabbable.CurrentGrabbers[1].Chirality, 0.3f, 0.5f);
						}
					}
				}
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