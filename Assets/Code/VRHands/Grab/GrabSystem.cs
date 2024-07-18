using System;
using BeauUtil;
using FieldDay.Debugging;
using FieldDay.Sockets;
using FieldDay.Systems;
using UnityEngine;

namespace FieldDay.VRHands {
    [SysUpdate(GameLoopPhase.FixedUpdate, 500)]
    public class GrabSystem : ComponentSystemBehaviour<Grabber> {
        static private readonly Collider[] OverlapWorkArray = new Collider[32];

        public override void ProcessWorkForComponent(Grabber component, float deltaTime) {
            switch (component.State) {
                case GrabberState.AttemptRelease: {
                    Release(component);
                    break;
                }

                case GrabberState.AttemptGrab: {
                    AttemptGrab(component);
                    break;
                }

                case GrabberState.Holding: {
                    UpdateHolding(component);
                    break;
                }
            }
        }

        protected override void OnComponentRemoved(Grabber component) {
            GrabUtility.DropCurrent(component, false);
        }

        static private void UpdateHolding(Grabber grabber) {
            // if object is deleted, deactivated, or otherwise set to not be grabbable
            if (!grabber.HeldObject || !grabber.HeldObject.GrabEnabled || !grabber.HeldObject.isActiveAndEnabled || (grabber.HeldObjectSnapNodeIndex >= 0 && grabber.HeldObject.DisabledSnapNodes[grabber.HeldObjectSnapNodeIndex]) || (grabber.Joint && !grabber.Joint.connectedBody)) {
                GrabUtility.DropCurrent(grabber, false);
            } else {
                DebugDraw.AddPoint(grabber.CachedTransform.TransformPoint(grabber.Joint.anchor), 0.05f, Color.yellow);
                DebugDraw.AddPoint(grabber.Joint.connectedBody.transform.TransformPoint(grabber.Joint.connectedAnchor), 0.05f, Color.green);
            }
        }

        static private void AttemptGrab(Grabber grabber) {
            grabber.GripCenter.GetPositionAndRotation(out Vector3 gripCenter, out Quaternion gripRotation);

            DebugDraw.AddSphere(gripCenter, grabber.GripRadius, Color.red.WithAlpha(0.5f), 1);

            int overlapCount = UnityEngine.Physics.OverlapSphereNonAlloc(gripCenter, grabber.GripRadius, OverlapWorkArray, grabber.GripMask);
            Grabbable closest = null;
            int snapIndex = -1;
            if (overlapCount > 0) {
                closest = ClosestGrabbable(gripCenter, grabber, grabber.CanGrab, OverlapWorkArray, overlapCount);
                if (closest) {
                    if (!GrabUtility.TryFindClosestSnapNode(closest, gripCenter, gripRotation, grabber.Chirality, out snapIndex) && closest.MustGrabAtSnap) {
                        closest = null;
                    }
                }
            }
            Array.Clear(OverlapWorkArray, 0, overlapCount);

            if (closest == null || !GrabUtility.TryGrab(grabber, closest, snapIndex)) {
                grabber.State = GrabberState.Empty;
                grabber.OnGrabFailed.Invoke();
            }
        }

        static private Grabbable ClosestGrabbable(Vector3 center, Grabber grabber, GrabberPredicate predicate, Collider[] overlaps, int overlapCount) {
            Grabbable bestGrabbable = null;
            float bestDistSq = float.MaxValue;
            
            Collider check;
            Grabbable checkGrabbable;

            for(int i = 0; i < overlapCount; i++) {
                check = overlaps[i];
                if ((checkGrabbable = check.GetComponentInParent<Grabbable>()) != null 
                    && grabber.HeldObject != checkGrabbable
                    && predicate(grabber, checkGrabbable)) {
                    float distSq = Vector3.SqrMagnitude(check.ClosestPoint(center) - center);
                    if (distSq < bestDistSq) {
                        bestGrabbable = checkGrabbable;
                        bestDistSq = distSq;
                    }
                }
            }

            return bestGrabbable;
        }

        static private void Release(Grabber grabber) {
            if (grabber.HeldObject && grabber.HeldObject.TryGetComponent(out Socketable socketable) && socketable.HighlightedSocket) {
                if (!SocketUtility.TryAddToSocket(socketable, socketable.HighlightedSocket, false)) {
                    GrabUtility.DropCurrent(grabber, true);
                }
            } else {
                GrabUtility.DropCurrent(grabber, true);
            }
            grabber.State = GrabberState.Empty;
        }
    }
}