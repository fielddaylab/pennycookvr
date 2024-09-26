using FieldDay;
using FieldDay.Systems;
using UnityEngine;

namespace Pennycook {
    [SysUpdate(GameLoopPhase.FixedUpdate, 500)]
    public sealed class PlayerColliderApproximationSystem : SharedStateSystemBehaviour<PlayerRig> {
        public override void ProcessWork(float deltaTime) {
            Vector3 headPos = m_State.HeadRoot.localPosition;

            Vector3 bodyPos = headPos;
            bodyPos.y /= 2;

            if (headPos.y > 0) {
                m_State.BodyCollider.height = headPos.y;
                m_State.BodyCollider.transform.localPosition = bodyPos;
                m_State.BodyCollider.enabled = true;
            } else {
                m_State.BodyCollider.enabled = false;
            }
        }
    }
}