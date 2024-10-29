using BeauUtil;
using FieldDay.Systems;
using FieldDay.HID.XR;
using FieldDay.XR;
using UnityEngine;

namespace FieldDay.VRHands {
    [SysUpdate(GameLoopPhase.Update, 1000)]
    public class GrabPosedHandSystem : ComponentSystemBehaviour<GrabPosedHand> {
        public override void ProcessWorkForComponent(GrabPosedHand component, float deltaTime) {
            bool isPosed = component.Grabber.HeldObject && component.Grabber.HeldObjectSnapNodeIndex >= 0;
            bool stateModified = Ref.Replace(ref component.WasGripPosed, isPosed);

            if (stateModified) {
                if (component.CachedTracked) {
                    component.CachedTracked.TrackingEnabled = !isPosed;
                }
            }

            if (isPosed) {
                Pose pose = GrabUtility.ResolveSnapNodePose(component.Grabber.HeldObject, component.Grabber.HeldObjectSnapNodeIndex, component.Grabber, component.CachedTransform.localScale.x);
                component.CachedTransform.SetPositionAndRotation(pose.position, pose.rotation * component.Rotation);
            }

            if (component.Animator) {

                XRInputState input = Find.State<XRInputState>();

                if(component.Grabber.Chirality == XRHandIndex.Left) {
                    if(component.Grabber.HeldObject != null) {
                        float snapGrip = component.Grabber.HeldObject.GrabberAnim.GripAnimStrength;
                        if(snapGrip != 0f) {
                            component.AnimateGrip(snapGrip);
                        }
                        else {
                            component.AnimateGrip(input.LeftHand.Axis.Grip);
                        }
                    } else {
                        component.AnimateGrip(input.LeftHand.Axis.Grip);
                    }
                } else if(component.Grabber.Chirality == XRHandIndex.Right) {
                    if(component.Grabber.HeldObject != null) {
                        float snapGrip = component.Grabber.HeldObject.GrabberAnim.GripAnimStrength;
                        if(snapGrip != 0f) {
                            component.AnimateGrip(snapGrip);
                        }
                        else {
                            component.AnimateGrip(input.RightHand.Axis.Grip);
                        }
                    } else {
                        component.AnimateGrip(input.RightHand.Axis.Grip);
                    }
                }
            }
        }
    }
}