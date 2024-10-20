using System;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.HID;
using FieldDay.HID.XR;
using FieldDay.SharedState;
using UnityEngine;
using UnityEngine.XR;

namespace FieldDay.XR {
    public class XRInputState : SharedStateComponent {
        #region Configuration

        [AutoEnum] public XRHandButtons TrackedButtonMask = XRHandButtons.All;
        [AutoEnum] public XRHandAxes TrackedAxisMask = XRHandAxes.All;
        public XRHandAxisDeadzones TrackedAxisDeadzones = XRHandAxisDeadzones.Default;

        #endregion // Configuration

        [NonSerialized] public XRSourceMask AvailableSources;
        [NonSerialized] public bool HeadPresence;

        [NonSerialized] public Pose LeftEye;
        [NonSerialized] public Pose RightEye;
        [NonSerialized] public Pose CenterEye;
        [NonSerialized] public Pose Head;

        [NonSerialized] public ushort LastControllerUpdateFrame;
        [NonSerialized] public XRHandState LeftHand;
        [NonSerialized] public XRHandState RightHand;

        [NonSerialized] public XRHapticsRequest LeftHaptics;
        [NonSerialized] public XRHapticsRequest RightHaptics;

        public bool IsAvailable(XRNode node) {
            return (AvailableSources & (XRSourceMask) (1 << (int) node)) != 0;
        }

        public bool IsAvailable(XRSourceMask nodeMask) {
            return (AvailableSources & nodeMask) != 0;
        }

        public ref XRHandState Hand(XRHandIndex hand) {
            Assert.True(hand == XRHandIndex.Left || hand == XRHandIndex.Right);
            if (hand == XRHandIndex.Left) {
                return ref LeftHand;
            } else {
                return ref RightHand;
            }
        }
    }

    public struct XRHandState {
        public Pose Pose;
        public DigitalControlStates<XRHandButtons> Buttons;
        public XRHandAxisFrame Axis;
    }

    public struct XRHapticsRequest {
        public float Amplitude;
        public float Duration;
    }

    static public class XRInputUtility {
        static public readonly CastableEvent<bool> OnUserPresenceUpdated = new CastableEvent<bool>(4);
        static public readonly CastableEvent<XRSourceMask> OnAvailableNodesUpdated = new CastableEvent<XRSourceMask>(4);

        static public void RequestHaptics(XRHandIndex hand, float amp, float duration) {
            var request = new XRHapticsRequest() {
                Amplitude = amp,
                Duration = duration
            };

            var state = Find.State<XRInputState>();

            if (hand == XRHandIndex.Left) {
                state.LeftHaptics = request;
            }
            if (hand == XRHandIndex.Right) {
                state.RightHaptics = request;
            }
        }
    }
}