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

            int collided = Physics.OverlapSphereNonAlloc(m_StateA.BodyCollider.transform.position, 1, s_ColliderWorkList, LayerMasks.Warpable_Mask, QueryTriggerInteraction.Collide);
            if (collided > 0) {
                TabletWarpPoint warpPoint = s_ColliderWorkList[0].GetComponentInParent<TabletWarpPoint>();
                if (warpPoint) {
                    PlayerMovementUtility.SetCurrentWarp(m_StateB, warpPoint);
                }
                Array.Clear(s_ColliderWorkList, 0, collided);
            }
        }
    }
}