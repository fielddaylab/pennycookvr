using FieldDay;
using FieldDay.HID.XR;
using FieldDay.Systems;
using FieldDay.XR;
using UnityEngine;

namespace Pennycook {
    [SysUpdate(GameLoopPhase.FixedUpdate, 10)]
    public class PlayerMovementSystem : SharedStateSystemBehaviour<PlayerRig, XRInputState> {
        public override void ProcessWork(float deltaTime) {
            var eitherHandButtons = m_StateB.LeftHand.Buttons | m_StateB.RightHand.Buttons;
            if (eitherHandButtons.ConsumePress(XRHandButtons.PrimaryAxisLeft)) {
                m_StateA.MoveRoot.Rotate(new Vector3(0, -30, 0), Space.Self);
            }
            if (eitherHandButtons.ConsumePress(XRHandButtons.PrimaryAxisRight)) {
                m_StateA.MoveRoot.Rotate(new Vector3(0, 30, 0), Space.Self);
            }

#if UNITY_EDITOR

            Vector3 flattenedLook = m_StateA.HeadLook.forward;
            flattenedLook.y = 0;
            flattenedLook.Normalize();

            if (eitherHandButtons.ConsumePress(XRHandButtons.PrimaryAxisUp)) {
                m_StateA.MoveRoot.Translate(flattenedLook * 0.3f, Space.World);
            }
            if (eitherHandButtons.ConsumePress(XRHandButtons.PrimaryAxisDown)) {
                m_StateA.MoveRoot.Translate(flattenedLook * -0.3f, Space.World);
            }

            if (m_StateB.LeftHand.Buttons.IsDownAll(XRHandButtons.PrimaryAxisClick | XRHandButtons.TriggerButton)
                && m_StateB.RightHand.Buttons.IsDownAll(XRHandButtons.PrimaryAxisClick | XRHandButtons.TriggerButton)) {
                Debug.Break();
            }

#endif // UNITY_EDITOR
        }
    }
}