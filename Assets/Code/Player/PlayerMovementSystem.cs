using FieldDay;
using FieldDay.HID;
using FieldDay.HID.XR;
using FieldDay.Systems;
using FieldDay.XR;
using UnityEngine;
using Pennycook.Tablet;

namespace Pennycook {
    [SysUpdate(GameLoopPhase.Update, 10)]
    public class PlayerMovementSystem : SharedStateSystemBehaviour<PlayerRig, XRInputState, PlayerMovementState> {
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
            flattenedLook *= 0.8f;

            if (m_StateB.LeftHand.Buttons.ConsumePress(XRHandButtons.PrimaryAxisUp)) {
                using (var move = new PlayerRigUtils.MovementRequest(m_StateA)) {
                    if(TryMove(move.Rig.HeadRoot.position, flattenedLook, m_StateC.CurrentWarp)) {
                        move.Translate(flattenedLook);
                        TryMoveCase(move.Rig.HeadRoot.position, flattenedLook, flattenedLook, m_StateC.CurrentWarp);
                    }
                }
            }
            if (m_StateB.LeftHand.Buttons.ConsumePress(XRHandButtons.PrimaryAxisDown)) {
                using (var move = new PlayerRigUtils.MovementRequest(m_StateA)) {
                    if(TryMove(move.Rig.HeadRoot.position, -flattenedLook, m_StateC.CurrentWarp)) {
                        move.Translate(-flattenedLook);
                        TryMoveCase(move.Rig.HeadRoot.position, flattenedLook, -flattenedLook, m_StateC.CurrentWarp);
                    }
                }
            }

            if (m_StateB.RightHand.Buttons.ConsumePress(XRHandButtons.PrimaryAxisUp)) {
                using (var move = new PlayerRigUtils.MovementRequest(m_StateA)) {
                    if(TryMove(move.Rig.HeadRoot.position, flattenedLook, m_StateC.CurrentWarp)) {
                        move.Translate(flattenedLook);
                        TryMoveCase(move.Rig.HeadRoot.position, flattenedLook, flattenedLook, m_StateC.CurrentWarp);
                    }
                }
            }
            if (m_StateB.RightHand.Buttons.ConsumePress(XRHandButtons.PrimaryAxisDown)) {
                using (var move = new PlayerRigUtils.MovementRequest(m_StateA)) {
                    if(TryMove(move.Rig.HeadRoot.position, -flattenedLook, m_StateC.CurrentWarp)) {
                        move.Translate(-flattenedLook);
                        TryMoveCase(move.Rig.HeadRoot.position, flattenedLook, -flattenedLook, m_StateC.CurrentWarp);
                    }
                }
            }

#if UNITY_EDITOR

            if (m_StateB.LeftHand.Buttons.IsDownAll(XRHandButtons.PrimaryAxisClick | XRHandButtons.TriggerButton)
                && m_StateB.RightHand.Buttons.IsDownAll(XRHandButtons.PrimaryAxisClick | XRHandButtons.TriggerButton)) {
                Debug.Break();
            }

#endif // UNITY_EDITOR
        }

        private void TryMoveCase(Vector3 root, Vector3 look, Vector3 translation, TabletWarpPoint warpPoint) {
            if(warpPoint.Group == TabletWarpPointGroup.Rookery) {
                Vector3 caseToPlayer = root - warpPoint.Case.transform.position;
                caseToPlayer.y = 0f;
                caseToPlayer = Vector3.Normalize(caseToPlayer);

                if(Vector3.Dot(caseToPlayer, look) > 0f) {
                    Vector3 flatRoot = root;
                    flatRoot.y = warpPoint.Case.transform.position.y;
                    if(Vector3.Distance(flatRoot, warpPoint.Case.transform.position) > 4f) {
                        warpPoint.Case.transform.position += translation;
                    }
                } else {
                     if(Vector3.Dot(caseToPlayer, translation) > 0f) {
                        Vector3 flatRoot = root;
                        flatRoot.y = warpPoint.Case.transform.position.y;
                        if(Vector3.Distance(flatRoot, warpPoint.Case.transform.position) > 4f) {
                            warpPoint.Case.transform.position += translation;
                        }
                     }
                }
            }
        }

        private bool TryMove(Vector3 root, Vector3 translation, TabletWarpPoint warpPoint) {
            
			if(warpPoint == null)
			{
				return true;
			}
			
			Vector3 flatRoot = root;
			
            flatRoot.y = warpPoint.transform.position.y;
            if(Vector3.Distance(flatRoot+translation, warpPoint.transform.position) < warpPoint.Radius) {
                return true;
            }

            return false;
        }
    }
}