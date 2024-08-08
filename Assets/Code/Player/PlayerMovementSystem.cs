using FieldDay;
using FieldDay.HID;
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
                using (var move = new PlayerRigUtils.MovementRequest(m_StateA)) {
                    move.Rotate(new Vector3(0, -30, 0));
                }
            }
            if (eitherHandButtons.ConsumePress(XRHandButtons.PrimaryAxisRight)) {
                using (var move = new PlayerRigUtils.MovementRequest(m_StateA)) {
                    move.Rotate(new Vector3(0, 30, 0));
                }
            }

#if UNITY_EDITOR

            Vector3 flattenedLook = m_StateA.HeadLook.forward;
            flattenedLook.y = 0;
            flattenedLook.Normalize();

            var notGrippedHands = default(DigitalControlStates<XRHandButtons>);
            if (!m_StateA.LeftHand.Grabber.HeldObject) {
                notGrippedHands |= m_StateB.LeftHand.Buttons;
            }
            if (!m_StateA.RightHand.Grabber.HeldObject) {
                notGrippedHands |= m_StateB.RightHand.Buttons;
            }

            if (notGrippedHands.ConsumePress(XRHandButtons.PrimaryAxisUp)) {
                using (var move = new PlayerRigUtils.MovementRequest(m_StateA)) {
                    move.Translate(flattenedLook * 0.3f);
                }
            }
            if (notGrippedHands.ConsumePress(XRHandButtons.PrimaryAxisDown)) {
                using (var move = new PlayerRigUtils.MovementRequest(m_StateA)) {
                    move.Translate(flattenedLook * -0.3f);
                }
            }

            if (m_StateB.LeftHand.Buttons.IsDownAll(XRHandButtons.PrimaryAxisClick | XRHandButtons.TriggerButton)
                && m_StateB.RightHand.Buttons.IsDownAll(XRHandButtons.PrimaryAxisClick | XRHandButtons.TriggerButton)) {
                Debug.Break();
            }

#endif // UNITY_EDITOR
        }
    }
}