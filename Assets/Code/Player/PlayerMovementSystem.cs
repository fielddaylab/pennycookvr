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

            if (!m_StateA.LeftHand.Grabber.HeldObject && m_StateB.LeftHand.Buttons.IsDown(XRHandButtons.Primary)) {
                float scaleChange = 0;
                if (m_StateB.LeftHand.Buttons.IsDown(XRHandButtons.GripButton)) {
                    scaleChange = -m_StateB.LeftHand.Axis.Grip;
                }
                if (m_StateB.LeftHand.Buttons.IsDown(XRHandButtons.TriggerButton)) {
                    scaleChange = m_StateB.LeftHand.Axis.Trigger;
                }

                if (scaleChange != 0) {
                    float myScale = m_StateA.ScaleRoot.localScale.x;
                    myScale = Mathf.Clamp(myScale + scaleChange * deltaTime, 0.01f, 15);
                    m_StateA.ScaleRoot.localScale = new Vector3(myScale, myScale, myScale);
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