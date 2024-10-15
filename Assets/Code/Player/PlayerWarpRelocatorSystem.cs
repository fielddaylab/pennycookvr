using System;
using FieldDay;
using FieldDay.HID;
using FieldDay.HID.XR;
using FieldDay.Systems;
using FieldDay.XR;
using Pennycook.Tablet;
using UnityEngine;

namespace Pennycook {
    [SysUpdate(GameLoopPhase.Update, 11)]
    public class PlayerWarpRelocatorSystem : SharedStateSystemBehaviour<PlayerRig, PlayerMovementState> {
        static private Collider[] s_ColliderWorkList = new Collider[2];

        public override void ProcessWork(float deltaTime) {
            if (!Frame.Interval(10)) {
                return;
            }

            int collided = Physics.OverlapSphereNonAlloc(m_StateA.BodyCollider.transform.position, 1.2f, s_ColliderWorkList, LayerMasks.Warpable_Mask, QueryTriggerInteraction.Collide);
            if (collided > 0) {
                TabletWarpPoint warpPoint = s_ColliderWorkList[0].GetComponentInParent<TabletWarpPoint>();
                if (warpPoint) {
                    PlayerMovementUtility.SetCurrentWarp(m_StateB, warpPoint);
                }
                Array.Clear(s_ColliderWorkList, 0, collided);
            } else {
                collided = Physics.OverlapSphereNonAlloc(m_StateA.BodyCollider.transform.position, 0.08f, s_ColliderWorkList, LayerMasks.Solid_Mask | LayerMasks.RestrictWalk_Mask, QueryTriggerInteraction.Collide);
                if (collided > 0) {
                    TabletWarpPoint warpPoint = m_StateB.CurrentWarp;
                    if (warpPoint) {
                        PlayerMovementUtility.WarpTo(m_StateB, warpPoint);
                    }
                    Array.Clear(s_ColliderWorkList, 0, collided);
                }
            }
        }
    }
}