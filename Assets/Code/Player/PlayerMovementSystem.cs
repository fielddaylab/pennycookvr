using FieldDay;
using FieldDay.HID;
using FieldDay.HID.XR;
using FieldDay.Systems;
using FieldDay.XR;
using UnityEngine;

namespace Pennycook {
    [SysUpdate(GameLoopPhase.Update, 10)]
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

            Vector3 flattenedLook = m_StateA.HeadLook.forward;
            flattenedLook.y = 0;
            flattenedLook.Normalize();

            if (m_StateB.LeftHand.Buttons.ConsumePress(XRHandButtons.PrimaryAxisUp)) {
                using (var move = new PlayerRigUtils.MovementRequest(m_StateA)) {
                    move.Translate(flattenedLook * 0.3f);
                }
            }
            if (m_StateB.LeftHand.Buttons.ConsumePress(XRHandButtons.PrimaryAxisDown)) {
                using (var move = new PlayerRigUtils.MovementRequest(m_StateA)) {
                    move.Translate(flattenedLook * -0.3f);
                }
            }

            if (m_StateB.RightHand.Buttons.ConsumePress(XRHandButtons.PrimaryAxisUp)) {
                using (var move = new PlayerRigUtils.MovementRequest(m_StateA)) {
                    move.Translate(flattenedLook * 0.3f);
                }
            }
            if (m_StateB.RightHand.Buttons.ConsumePress(XRHandButtons.PrimaryAxisDown)) {
                using (var move = new PlayerRigUtils.MovementRequest(m_StateA)) {
                    move.Translate(flattenedLook * -0.3f);
                }
            }
            //PenguinNav.IsWalkable()
            
            if (m_StateB.LeftHand.Buttons.IsDownAll(XRHandButtons.PrimaryAxisClick | XRHandButtons.TriggerButton)
                && m_StateB.RightHand.Buttons.IsDownAll(XRHandButtons.PrimaryAxisClick | XRHandButtons.TriggerButton)) {
                Debug.Break();
            }
        }
    }
}