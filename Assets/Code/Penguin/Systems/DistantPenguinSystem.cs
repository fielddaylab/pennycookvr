using UnityEngine;
using FieldDay;
using FieldDay.Systems;
using BeauUtil;

namespace Pennycook {
    [SysUpdate(GameLoopPhase.LateUpdate)]
    public class DistantPenguinSystem : ComponentSystemBehaviour<DistantPenguin> {
        public override void ProcessWork(float deltaTime) {
            float currentTime = Time.time;

            for (int i = 0; i < m_Components.Count; i++) {
                if (!Frame.Interval(3, i)) {
                    continue;
                }

                DistantPenguin penguin = m_Components[i];
                penguin.MoveRoot.GetPositionAndRotation(out Vector3 currentPos, out Quaternion currentRot);

                float cycleOffsetX = penguin.RandomCycle;
                float cycleOffsetZ = cycleOffsetX * 1.3f;
                float speedMult = penguin.SpeedMult;
                float distMult = penguin.DistanceMult;

                Vector3 originalPos = penguin.OriginalPos;
                Vector3 nextPos;
                nextPos.x = originalPos.x
                    + distMult * 2 * Mathf.Sin(cycleOffsetX - (13 + currentTime * speedMult * Mathf.PI * 2 / 17.3f))
                    + 0.37f * Mathf.Cos(cycleOffsetX - 4 + (7 + currentTime * Mathf.PI * 2 / 9.2f));
                nextPos.y = currentPos.y;
                nextPos.z = originalPos.z
                    + distMult * 2 * Mathf.Cos(cycleOffsetZ + (currentTime * speedMult * Mathf.PI * 2 / 29.7f))
                    + 0.24f * Mathf.Sin(cycleOffsetZ + 3.7f + (13 + currentTime * Mathf.PI * 2 / 9.7f));

                Vector3 delta = nextPos - currentPos;
                Vector3 up;
                up.y = 1;
                up.x = 0.2f * Mathf.Sin(cycleOffsetZ * 1.8f - (currentTime * speedMult * Mathf.PI * 2 / 1.5f));
                up.z = 0;
                up.Normalize();

                Quaternion nextRot = currentRot;
                if (delta.sqrMagnitude > 0) {
                    //delta.y = delta.y + 0.02f * Mathf.Cos(cycleOffsetX + (10 + currentTime * speedMult * Mathf.PI * 2 / 1.9f));
                    nextRot = Quaternion.LookRotation(delta.normalized, up);
                }

                penguin.MoveRoot.SetPositionAndRotation(nextPos, nextRot);
            }
        }

        protected override void OnComponentAdded(DistantPenguin component) {
            component.RandomCycle = RNG.Instance.NextFloat(Mathf.PI * 2);
            component.DistanceMult = RNG.Instance.NextFloat(0.9f, 1.1f);
            component.SpeedMult = RNG.Instance.NextFloat(0.8f, 1.3f);
        }
    }
}