using System;
using System.Collections.Generic;
using BeauUtil;
using FieldDay.Components;
using FieldDay.VRHands;
using UnityEngine;

namespace FieldDay.Sockets {
    [RequireComponent(typeof(Rigidbody))]
    public class Socketable : BatchedComponent {
        public bool SocketEnabled = true;

        [NonSerialized] public Transform CachedTransform;
        [NonSerialized] public Rigidbody CachedRB;
        [NonSerialized] public Transform OriginalParent;

        [NonSerialized] public ObjectSocket CurrentSocket;
        [NonSerialized] public ObjectSocket LastSocket;

        [NonSerialized] public ObjectSocket OriginalSocket;

        [NonSerialized] public HashSet<ObjectSocket> PotentialSockets = new HashSet<ObjectSocket>();
        [NonSerialized] public ObjectSocket HighlightedSocket;

        #region Events

        public readonly CastableEvent<ObjectSocket> OnSocketHighlight = new CastableEvent<ObjectSocket>();
        public readonly CastableEvent<ObjectSocket> OnAddedToSocket = new CastableEvent<ObjectSocket>();
        public readonly CastableEvent<ObjectSocket> OnRemovedFromSocket = new CastableEvent<ObjectSocket>();

        #endregion // Events

        private void Awake() {
            this.CacheComponent(ref CachedTransform);
            this.CacheComponent(ref CachedRB);

            OriginalParent = CachedTransform.parent;
        }
    }

    static public class SocketUtility {
        #region Add

        /// <summary>
        /// Attempts to add the given socketable to a socket.
        /// </summary>
        static public bool TryAddToSocket(Socketable socketable, ObjectSocket socket, bool force) {
            if (!socketable) {
                return false;
            }

            if (!force && (socket.Locked || socket.Current || !socket.CanAdd(socket, socketable))) {
                return false;
            }

            if (socket.Current != socketable) {
                ReleaseCurrent(socket, socket.Current != socketable);
            }

            if (socketable.CurrentSocket != null && socketable.CurrentSocket != socket) {
                ReleaseCurrent(socketable.CurrentSocket, false);
            }

            socket.Current = socketable;
            socketable.CurrentSocket = socket;
            socketable.LastSocket = socket;

            if (socketable.TryGetComponent(out Grabbable grabbable)) {
                GrabUtility.DetachAll(grabbable);
            }

            socket.Location.GetPositionAndRotation(out Vector3 socketLocationPos, out Quaternion socketLocationRot);
            socketable.CachedTransform.SetPositionAndRotation(socketLocationPos, socketLocationRot);

            switch (socket.Mode) {
                case SocketMode.Reparent: {
                    socketable.CachedTransform.SetParent(socket.Location, true);
                    socketable.CachedRB.isKinematic = true;
                    break;
                }

                case SocketMode.FixedJoint: {
                    if (!socket.CurrentJoint) {
                        socket.CurrentJoint = socket.gameObject.AddComponent<FixedJoint>();
                    }
                    socket.CurrentJoint.connectedBody = socketable.CachedRB;
                    socket.JointConfig.Apply(socket.CurrentJoint);
                    break;
                }
            }

            socketable.OnAddedToSocket.Invoke(socket);
            socket.OnAdded.Invoke(socketable);
            OnObjectAddedToSocket.Invoke(socketable, socket);

            return true;
        }

        #endregion // Add

        #region Release

        /// <summary>
        /// Attempts to release the given socketable from its socket.
        /// </summary>
        static public bool TryReleaseFromCurrentSocket(Socketable socketable, bool applyReleaseForce) {
            if (socketable.CurrentSocket) {
                if (socketable.CurrentSocket.Locked) {
                    return false;
                }

                ReleaseCurrent(socketable.CurrentSocket, applyReleaseForce);
            }

            return true;
        }

        /// <summary>
        /// Attempts to release the given grabbable from its socket, if it has a socketable components.
        /// </summary>
        static public bool TryReleaseFromCurrentSocket(Grabbable grabbable, bool applyReleaseForce) {
            if (!grabbable.TryGetComponent(out Socketable socketable)) {
                return true;
            }

            return TryReleaseFromCurrentSocket(socketable, applyReleaseForce);
        }

        /// <summary>
        /// Releases the current socketed object.
        /// </summary>
        static public void ReleaseCurrent(ObjectSocket socket, bool applyReleaseForce) {
            Socketable cachedCurrent = socket.Current;

            if (!cachedCurrent) {
                return;
            }

            if (socket.CurrentJoint) {
                Rigidbody connected = socket.CurrentJoint.connectedBody;

                if (applyReleaseForce && connected != null) {
                    Vector3 force = socket.ReleaseForce;
                    force = socket.Location.TransformDirection(force);
                    connected.AddForce(force, ForceMode.Impulse);
                }

                Joint.Destroy(socket.CurrentJoint);
                socket.CurrentJoint = null;
            }

            switch (socket.Mode) {
                case SocketMode.Reparent: {
                    cachedCurrent.CachedTransform.SetParent(socket.Current.OriginalParent, true);

                    if (cachedCurrent.TryGetComponent(out Grabbable grabbable)) {
                        cachedCurrent.CachedRB.isKinematic = grabbable.DefaultRBKinematic;
                    } else {
                        cachedCurrent.CachedRB.isKinematic = false;
                    }

                    if (applyReleaseForce) {
                        Vector3 force = socket.ReleaseForce;
                        force = socket.Location.TransformDirection(force);
                        cachedCurrent.CachedRB.AddForce(force, ForceMode.Impulse);
                    }
                    break;
                }
            }

            socket.Current = null;
            cachedCurrent.CurrentSocket = null;

            cachedCurrent.OnRemovedFromSocket.Invoke(socket);
            socket.OnRemoved.Invoke(cachedCurrent);
            OnObjectRemovedFromSocket.Invoke(cachedCurrent, socket);
        }

        #endregion // Release

        #region Events

        static public readonly CastableEvent<Socketable, ObjectSocket> OnObjectAddedToSocket = new CastableEvent<Socketable, ObjectSocket>();
        static public readonly CastableEvent<Socketable, ObjectSocket> OnObjectRemovedFromSocket = new CastableEvent<Socketable, ObjectSocket>();

        #endregion // Events
    
        /// <summary>
        /// Sets the original socket for a given socketable object.
        /// </summary>
        static public void SetHomeSocket(Socketable socketable, ObjectSocket socket) {
            socketable.OriginalSocket = socket;
        }
    }
}