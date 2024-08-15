using UnityEngine;
using BeauUtil;
using FieldDay.HID.XR;
using FieldDay.Data;
using BeauUtil.Debugger;
using Unity.IL2CPP.CompilerServices;
using System;
using FieldDay.Sockets;
using FieldDay.Physics;

namespace FieldDay.VRHands {
    static public class GrabUtility {
        #region Snap Nodes

        static public bool TryFindClosestSnapNode(Grabbable grabbable, Vector3 worldPos, Quaternion worldRot, XRHandIndex handType, out int gripNodeIndex) {
            if (grabbable.SnapNodes.Length <= 0) {
                gripNodeIndex = -1;
                return false;
            }

            int closestIdx = -1;
            float closestDistSq = float.MaxValue;

            Vector3 worldLook = Geom.Forward(worldRot);

            ProcessClosestSnapNodesStatic(grabbable, worldPos, worldLook, grabbable.BothSnapNodeRange, ref closestIdx, ref closestDistSq);
            ProcessClosestSnapNodesDynamic(grabbable, worldPos, worldLook, grabbable.DynamicBothSnapNodeRange, ref closestIdx, ref closestDistSq);

            if (handType == XRHandIndex.Left) {
                ProcessClosestSnapNodesStatic(grabbable, worldPos, worldLook, grabbable.LeftSnapNodeRange, ref closestIdx, ref closestDistSq);
                ProcessClosestSnapNodesDynamic(grabbable, worldPos, worldLook, grabbable.DynamicLeftSnapNodeRange, ref closestIdx, ref closestDistSq);
            } else if (handType == XRHandIndex.Right) {
                ProcessClosestSnapNodesStatic(grabbable, worldPos, worldLook, grabbable.RightSnapNodeRange, ref closestIdx, ref closestDistSq);
                ProcessClosestSnapNodesDynamic(grabbable, worldPos, worldLook, grabbable.DynamicRightSnapNodeRange, ref closestIdx, ref closestDistSq);
            }

            if (closestIdx >= 0) {
                gripNodeIndex = closestIdx;
                return true;
            }

            gripNodeIndex = -1;
            return false;
        }

        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        static private void ProcessClosestSnapNodesStatic(Grabbable grabbable, Vector3 worldPos, Vector3 worldForward, OffsetLengthU16 range, ref int closestIdx, ref float closestDistSq) {
            if (range.Length <= 0) {
                return;
            }

            GrabbableSnapNodeData[] data = grabbable.SnapNodes;

            for (int i = range.Offset; i < range.End; i++) {
                if (grabbable.UsedSnapNodes[i] || grabbable.DisabledSnapNodes[i]) {
                    continue;
                }

                ref GrabbableSnapNodeData node = ref data[i];
                Assert.True((node.Flags & GrabbableSnapFlags.IsDynamic) == 0);
                Vector3 transformedPos = grabbable.CachedTransform.TransformPoint(node.RelativePose.position);
                Quaternion transformedRot = grabbable.CachedTransform.rotation * node.RelativePose.rotation;
                float align = Vector3.Dot(worldForward, Geom.Forward(transformedRot));

                if (align >= GrabConfig.MinAlignmentForSnap) {
                    float sqDist = Vector3.SqrMagnitude(transformedPos - worldPos);
                    if (sqDist < closestDistSq) {
                        closestIdx = i;
                        closestDistSq = sqDist;
                    }
                }
            }
        }

        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        static private void ProcessClosestSnapNodesDynamic(Grabbable grabbable, Vector3 worldPos, Vector3 worldForward, OffsetLengthU16 range, ref int closestIdx, ref float closestDistSq) {
            if (range.Length <= 0) {
                return;
            }

            GrabbableSnapNodeData[] data = grabbable.SnapNodes;

            for (int i = range.Offset; i < range.End; i++) {
                if (grabbable.UsedSnapNodes[i] || grabbable.DisabledSnapNodes[i]) {
                    continue;
                }

                ref GrabbableSnapNodeData node = ref data[i];
                Assert.True((node.Flags & GrabbableSnapFlags.IsDynamic) != 0);
                node.DynamicPose.GetPositionAndRotation(out Vector3 transformedPos, out Quaternion transformedRot);
                float align = Vector3.Dot(worldForward, Geom.Forward(transformedRot));

                if (align >= GrabConfig.MinAlignmentForSnap) {
                    float sqDist = Vector3.SqrMagnitude(transformedPos - worldPos);
                    if (sqDist < closestDistSq) {
                        closestIdx = i;
                        closestDistSq = sqDist;
                    }
                }
            }
        }

        /// <summary>
        /// Resolves the pose for a snapping node.
        /// </summary>
        static public Pose ResolveSnapNodePose(Grabbable grabbable, int nodeIndex, Grabber grabberReference) {
            Assert.True(nodeIndex >= 0 && nodeIndex < grabbable.SnapNodes.Length);
            Pose p;
            var node = grabbable.SnapNodes[nodeIndex];
            if ((node.Flags & GrabbableSnapFlags.IsDynamic) != 0) {
                node.DynamicPose.GetPositionAndRotation(out p.position, out p.rotation);
            } else {
                p.position = grabbable.CachedTransform.TransformPoint(node.RelativePose.position);
                p.rotation = grabbable.CachedTransform.rotation * node.RelativePose.rotation;
            }

            if (!ReferenceEquals(grabberReference.CachedTransform, grabberReference.GripCenter)) {
                grabberReference.GripCenter.GetLocalPositionAndRotation(out Vector3 localGripPos, out Quaternion localGripRot);
                p.position -= grabberReference.CachedTransform.TransformVector(localGripPos);
                p.rotation = p.rotation * Quaternion.Inverse(localGripRot);
            }

            return p;
        }

        /// <summary>
        /// Resolves the name for a snapping node.
        /// </summary>
        static public StringHash32 ResolveSnapNodeName(Grabbable grabbable, int nodeIndex) {
            if (nodeIndex < 0 || nodeIndex >= grabbable.SnapNodes.Length) {
                return default(StringHash32);
            }

            return grabbable.SnapNodes[nodeIndex].Name;
        }

        /// <summary>
        /// Resolves the name of the currently gripped snap node.
        /// </summary>
        static public StringHash32 ResolveSnapNodeName(Grabber grabber) {
            if (grabber.HeldObject && grabber.HeldObjectSnapNodeIndex >= 0) {
                return ResolveSnapNodeName(grabber.HeldObject, grabber.HeldObjectSnapNodeIndex);
            }

            return default(StringHash32);
        }

        #endregion // Snap Nodes

        #region Grab

        /// <summary>
        /// Attempts to grab the given grabbable.
        /// </summary>
        static public bool TryGrab(Grabber grabber, Grabbable grabbable, int snapIndex = -1) {
            if (!grabbable || !grabbable.GrabEnabled || grabber.HeldObject == grabbable || !SocketUtility.TryReleaseFromCurrentSocket(grabbable, false)) {
                return false;
            }

            if (grabbable.CurrentGrabberCount >= grabbable.MaxGrabbers) {
                if (!DetachOldest(grabbable)) {
                    return false;
                }
            }

            if (CleanUpHeldObjectReference(grabber, out Grabbable releasedObj)) {
                grabber.OnRelease.Invoke(releasedObj);
                releasedObj.OnReleased.Invoke(grabber);
            }

            grabber.HeldObject = grabbable;
            grabber.HeldObjectSnapNodeIndex = snapIndex;
            grabber.HoldStartTime = Frame.Timestamp();
            grabber.State = GrabberState.Holding;

            grabbable.CurrentGrabbers[grabbable.CurrentGrabberCount++] = grabber;

            // snap grabber to snap node

            if (snapIndex >= 0) {
                Pose p = ResolveSnapNodePose(grabbable, snapIndex, grabber);
                grabber.CachedRB.position = p.position;
                grabber.CachedRB.rotation = p.rotation;
                grabber.CachedTransform.SetPositionAndRotation(p.position, p.rotation);
                grabbable.UsedSnapNodes.Set(snapIndex);
            }

            // TODO: custom joint/configurable joint?

            // configure joint
            if (!grabber.Joint) {
                grabber.Joint = grabber.gameObject.AddComponent<FixedJoint>();
                grabber.Joint.enableCollision = false;
                grabber.Joint.enablePreprocessing = false;
                grabber.Joint.autoConfigureConnectedAnchor = true;
            }

            grabber.Joint.connectedBody = grabbable.CachedRB;
            grabber.Joint.anchor = grabber.GripCenter.localPosition;

            SerializedFixedJoint jointConfig = grabber.JointConfig;
            jointConfig.ConnectedMassScale *= Mathf.Clamp(grabbable.CachedRB.mass, grabber.MinGripForce, grabber.MaxGripForce);
            if (grabbable.IsHeavy) {
                jointConfig.ConnectedMassScale *= grabber.HeavyGripForceMultiplier;
            }
            jointConfig.Apply(grabber.Joint);

            // TODO: configure animations

            grabber.OnGrab.Invoke(grabbable);
            grabbable.OnGrabbed.Invoke(grabber);

            return true;
        }

        #endregion // Grab

        #region Release

        /// <summary>
        /// Releases all grabbers holding on to the given grabbable.
        /// </summary>
        static public void DetachAll(Grabbable grabbable) {
            int dropIdx;
            while ((dropIdx = grabbable.CurrentGrabberCount - 1) >= 0) {
                DropCurrentWithIndex(grabbable.CurrentGrabbers[dropIdx], false, dropIdx);
            }
        }

        /// <summary>
        /// Drops the held grabbable from the given grabber.
        /// </summary>
        static public bool DropCurrent(Grabber grabber, bool applyReleaseForce) {
            return DropCurrentWithIndex(grabber, applyReleaseForce, -1);
        }

        // Detaches the least recent grabber
        static private bool DetachOldest(Grabbable grabbable) {
            if (grabbable.CurrentGrabberCount <= 0) {
                return false;
            }

            Grabber least = grabbable.CurrentGrabbers[0];
            int leastIdx = 0;
            long leastTS = least.HoldStartTime;

            for(int i = 1; i < grabbable.CurrentGrabberCount; i++) {
                Grabber check = grabbable.CurrentGrabbers[i];
                if (check.HoldStartTime < leastTS) {
                    least = check;
                    leastIdx = i;
                    leastTS = check.HoldStartTime;
                }
            }

            DropCurrentWithIndex(least, false, leastIdx);
            return true;
        }

        static private bool DropCurrentWithIndex(Grabber grabber, bool applyReleaseForce, int indexInArray) {
            bool detachedAnything = false;
            Grabbable releasedObj = null;

            detachedAnything = CleanUpHeldObjectReference(grabber, out releasedObj, indexInArray);
            detachedAnything |= CleanUpJoint(grabber, applyReleaseForce);

            if (detachedAnything) {
                grabber.State = GrabberState.Empty;
            }

            if (!ReferenceEquals(releasedObj, null)) {
                grabber.OnRelease.Invoke(releasedObj);
                releasedObj.OnReleased.Invoke(grabber);
            }

            return detachedAnything;
        }

        static private bool CleanUpJoint(Grabber grabber, bool applyReleaseForce) {
            // clean up joint
            if (!ReferenceEquals(grabber.Joint, null)) {
                if (grabber.Joint != null) {
                    if (applyReleaseForce && grabber.ReleaseThrowForce > 0) {
                        Rigidbody connected = grabber.Joint.connectedBody;
                        if (connected) {
                            Vector3 anchor = grabber.Joint.connectedAnchor;
                            anchor = connected.transform.TransformPoint(anchor);
                            Vector3 vel = grabber.CachedRB.velocity;

                            connected.AddForceAtPosition(vel * grabber.ReleaseThrowForce, anchor, ForceMode.Impulse);
                        }
                    }                    
                    grabber.Joint.connectedBody = null;
                    Joint.Destroy(grabber.Joint);
                }

                grabber.Joint = null;
                return true;
            }

            return false;
        }

        static private bool CleanUpHeldObjectReference(Grabber grabber, out Grabbable removedGrabbable, int arrayIndex = -1) {
            if (!ReferenceEquals(grabber.HeldObject, null)) {
                Grabbable cachedGrabbable = grabber.HeldObject;
                if (cachedGrabbable) {
                    if (arrayIndex < 0) {
                        arrayIndex = Array.IndexOf(cachedGrabbable.CurrentGrabbers, grabber);
                    }
                    Assert.True(arrayIndex >= 0);
                    ArrayUtils.FastRemoveAt(cachedGrabbable.CurrentGrabbers, ref cachedGrabbable.CurrentGrabberCount, arrayIndex);

                    if (grabber.HeldObjectSnapNodeIndex >= 0) {
                        cachedGrabbable.UsedSnapNodes.Unset(grabber.HeldObjectSnapNodeIndex);
                    }
                }

                grabber.HoldStartTime = -1;
                grabber.HeldObject = null;
                grabber.HeldObjectSnapNodeIndex = -1;

                removedGrabbable = cachedGrabbable;
                return true;
            }

            removedGrabbable = null;
            return false;
        }

        #endregion // Release

        #region Checks

        /// <summary>
        /// Returns if the given grabbable is being grabbed.
        /// </summary>
        static public bool IsGrabbed(Grabbable grabbable) {
            return grabbable.CurrentGrabberCount > 0;
        }

        /// <summary>
        /// Returns if the given grabbable is being grabbed by a specific hand.
        /// </summary>
        static public bool IsGrabbed(Grabbable grabbable, XRHandIndex hand) {
            if (hand == XRHandIndex.Any) {
                return grabbable.CurrentGrabberCount > 0;
            }

            for(int i = 0; i < grabbable.CurrentGrabberCount; i++) {
                if (grabbable.CurrentGrabbers[i].Chirality == hand) {
                    return true;
                }
            }

            return false;
        }

        #endregion // Checks
    }

    static public class GrabConfig {
        [ConfigVar("Minimum Alignment to Grab", -1, 1, 0.05f)]
        static public float MinAlignmentForSnap = -0.5f;
    }
}