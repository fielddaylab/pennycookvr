using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.HID.XR;
using FieldDay.Systems;
using UnityEngine;
using UnityEngine.XR;

namespace FieldDay.XR {
    [SysUpdate(GameLoopPhaseMask.DebugUpdate | GameLoopPhaseMask.FixedUpdate | GameLoopPhaseMask.Update | GameLoopPhaseMask.ApplicationPreRender, -8000, AllowExecutionDuringLoad = true)]
    public class XRInputUpdateSystem : SharedStateSystemBehaviour<XRInputState> {
        private readonly List<XRNodeState> m_NodeStateWorkList = new List<XRNodeState>(8);

        public override void ProcessWork(float deltaTime) {
            XRSourceMask sourcesAvailable = 0;

            XRInputState inputState = m_State;

            if (!XRSettings.isDeviceActive) {
                inputState.AvailableSources = 0;
                if (inputState.HeadPresence) {
                    Log.Msg("[XRInputUpdateSystem] User presence lost due to xr device loss!");
                    inputState.HeadPresence = false;
                    XRInputUtility.OnUserPresenceUpdated.Invoke(false);
                }
                return;
            }

            // update nodes

            InputTracking.GetNodeStates(m_NodeStateWorkList);

            for(int i = 0, len = m_NodeStateWorkList.Count; i < len; i++) {
                XRNodeState state = m_NodeStateWorkList[i];

                XRSourceMask mask = (XRSourceMask) (1 << (int) state.nodeType);

                if ((mask & XRSourceMask.Everything) != 0 && state.TryGetPose(out Pose pose)) {
                    sourcesAvailable |= mask;

                    switch (state.nodeType) {
                        case XRNode.LeftEye: {
                            inputState.LeftEye = pose;
                            break;
                        }
                        case XRNode.RightEye: {
                            inputState.RightEye = pose;
                            break;
                        }
                        case XRNode.CenterEye: {
                            inputState.CenterEye = pose;
                            break;
                        }
                        case XRNode.Head: {
                            inputState.Head = pose;
                            break;
                        }
                        case XRNode.LeftHand: {
                            inputState.LeftHand.Pose = pose;
                            break;
                        }
                        case XRNode.RightHand: {
                            inputState.RightHand.Pose = pose;
                            break;
                        }
                    }
                }
            }

            // update user presence

            InputDevice head = InputDevices.GetDeviceAtXRNode(XRNode.Head);

            if (head.isValid) {
                bool presence = head.GetFeature(CommonUsages.userPresence);
                if (Ref.Replace(ref inputState.HeadPresence, presence)) {
                    Log.Msg("[XRInputUpdateSystem] User presence {0}!", presence ? "found" : "lost");
                    XRInputUtility.OnUserPresenceUpdated.Invoke(presence);
                }
            } else if (inputState.HeadPresence) {
                Log.Msg("[XRInputUpdateSystem] User presence lost due to head tracking loss!");
                inputState.HeadPresence = false;
                XRInputUtility.OnUserPresenceUpdated.Invoke(false);
            }

            // update controllers

            InputDevice leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            InputDevice rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

            if (Ref.Replace(ref inputState.LastControllerUpdateFrame, Frame.Index)) {
                ReadHandController(ref inputState.LeftHand, leftHand, inputState.TrackedButtonMask, inputState.TrackedAxisMask, inputState.TrackedAxisDeadzones);
                ReadHandController(ref inputState.RightHand, rightHand, inputState.TrackedButtonMask, inputState.TrackedAxisMask, inputState.TrackedAxisDeadzones);
            }

            DispatchHaptics(ref m_State.LeftHaptics, leftHand);
            DispatchHaptics(ref m_State.RightHaptics, rightHand);

            m_NodeStateWorkList.Clear();
            if (inputState.AvailableSources != sourcesAvailable) {
                inputState.AvailableSources = sourcesAvailable;
                Log.Msg("[XRInputUpdateSystem] Available sources updated to {0}", sourcesAvailable);
                XRInputUtility.OnAvailableNodesUpdated.Invoke(sourcesAvailable);
            }
        }

        static private void ReadHandController(ref XRHandState state, InputDevice device, XRHandButtons buttonMask, XRHandAxes axisMask, in XRHandAxisDeadzones deadzones) {
            XRHandControllerFrame frame = XRUtility.ReadControllerState(device, buttonMask, axisMask, deadzones);
            state.Buttons.Update(frame.Buttons);
            state.Axis = frame.Axes;
        }

        static private void DispatchHaptics(ref XRHapticsRequest req, InputDevice device) {
            if (req.Duration > 0) {
                device.SendHapticImpulse(0, req.Amplitude, req.Duration);
                req = default;
            }
        }
    }
}