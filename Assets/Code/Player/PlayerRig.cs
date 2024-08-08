using System;
using BeauUtil;
using FieldDay.Physics;
using FieldDay.SharedState;
using FieldDay.VRHands;
using UnityEngine;

namespace Pennycook {
    public class PlayerRig : SharedStateComponent {
        public Transform MoveRoot;
        public Transform HeadRoot;
        public Transform HeadLook;

        public PlayerHandRig LeftHand;
        public PlayerHandRig RightHand;
    }

    static public class PlayerRigUtils {
        static public void SyncPhysicsHand(PlayerHandRig hand, bool drop) {
            if (drop) {
                GrabUtility.DropCurrent(hand.Grabber, false);
            }
            RBInterpolatorUtility.SyncInstant(hand.Interpolator);
        }

        static public void SyncPhysicsHands(PlayerRig rig, bool drop) {
            SyncPhysicsHand(rig.LeftHand, drop);
            SyncPhysicsHand(rig.RightHand, drop);
        }

        public struct MovementRequest : IDisposable {
            public struct ReparentRecord {
                public Transform Root;
                public Transform OriginalParent;
            }

            public readonly PlayerRig Rig;
            public ReparentRecord Left;
            public ReparentRecord Right;

            public MovementRequest(PlayerRig rig) {
                Rig = rig;
                Left = Right = default;
                PopulateHand(ref Left, rig.LeftHand.Grabber, rig.MoveRoot);
                PopulateHand(ref Right, rig.RightHand.Grabber, rig.MoveRoot);
            }

            public void Rotate(Vector3 rotateEuler) {
                Rig.MoveRoot.Rotate(rotateEuler, Space.Self);
            }

            public void Translate(Vector3 translation) {
                Rig.MoveRoot.Translate(translation, Space.World);
            }

            public void Teleport(Vector3 location) {
                Rig.MoveRoot.position = location;
            }

            public void Teleport(Vector3 location, Vector3 facing) {
                facing.y = 0;

                Vector3 headRot = Rig.HeadLook.localEulerAngles;
                headRot.x = headRot.z = 0;

                Quaternion finalRot = Quaternion.LookRotation(facing, Vector3.up) * Quaternion.Inverse(Quaternion.Euler(headRot));
                Rig.MoveRoot.SetPositionAndRotation(location, finalRot);
            }

            public void Dispose() {
                UnparentHand(ref Left);
                UnparentHand(ref Right);

                SyncPhysicsHands(Rig, false);
            }

            static private void PopulateHand(ref ReparentRecord grabbed, Grabber grabber, Transform reparent) {
                if (grabbed.Root) {
                    return;
                }

                if (grabber.HeldObject != null) {
                    grabbed.Root = grabber.HeldObject.CachedTransform;
                    grabbed.OriginalParent = grabbed.Root.parent;
                    grabbed.Root.SetParent(reparent, true);
                }
            }

            static private void UnparentHand(ref ReparentRecord grabbed) {
                if (!grabbed.Root) {
                    return;
                }

                grabbed.Root.SetParent(grabbed.OriginalParent, true);
                grabbed = default;
            }
        }
    }
}