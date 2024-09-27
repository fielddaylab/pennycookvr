using BeauUtil.Debugger;
using FieldDay.Systems;
using UnityEngine;
using UnityEngine.XR;

namespace FieldDay.XR {
    [SysUpdate(GameLoopPhaseMask.DebugUpdate | GameLoopPhaseMask.FixedUpdate | GameLoopPhaseMask.Update | GameLoopPhaseMask.ApplicationPreRender, -7000, AllowExecutionDuringLoad = true)]
    public class XRTrackedTransformSystem : ComponentSystemBehaviour<XRTrackedTransform> {
        public float WorldScale;

        public override void ProcessWork(float deltaTime) {
            XRInputState inputState = Find.State<XRInputState>();

            bool isPreRender = GameLoop.IsPhase(GameLoopPhase.ApplicationPreRender);

            foreach (var c in m_Components) {
                if (!c.TrackingEnabled || (c.RenderOnly && !isPreRender)) {
                    continue;
                }

                bool available = inputState.IsAvailable(c.Node);
                //if (!available) {
                //    continue;
                //}

                Transform t = c.transform;
                Pose p;
                t.GetLocalPositionAndRotation(out p.position, out p.rotation);

                switch (c.Node) {
                    case XRNode.Head:
                        p = inputState.Head;
                        break;
                    case XRNode.LeftEye:
                        p = inputState.LeftEye;
                        break;
                    case XRNode.RightEye:
                        p = inputState.RightEye;
                        break;
                    case XRNode.CenterEye:
                        p = inputState.CenterEye;
                        break;
                    case XRNode.LeftHand:
                        p = inputState.LeftHand.Pose;
                        break;
                    case XRNode.RightHand:
                        p = inputState.RightHand.Pose;
                        break;
                    default:
                        Log.Error("[XRTrackedTransformSystem] Cannot track node '{0}'", c.Node.ToString());
                        c.enabled = false;
                        break;
                }

                t.SetLocalPositionAndRotation(p.position * WorldScale, p.rotation * c.Rotation);
            }
        }
    }
}