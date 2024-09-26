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
                Pose pose = GrabUtility.ResolveSnapNodePose(component.Grabber.HeldObject, component.Grabber.HeldObjectSnapNodeIndex, component.Grabber);
                component.CachedTransform.SetPositionAndRotation(pose.position, pose.rotation * component.Rotation);
            }

            if (component.Animator) {

                XRInputState input = Find.State<XRInputState>();

                if(component.Grabber.Chirality == XRHandIndex.Left) {
                    component.AnimateGrip(input.LeftHand.Axis.Grip);
                } else if(component.Grabber.Chirality == XRHandIndex.Right) {
                    component.AnimateGrip(input.RightHand.Axis.Grip);
                }
            }
        }
    }
}