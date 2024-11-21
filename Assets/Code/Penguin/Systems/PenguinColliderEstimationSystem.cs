using UnityEngine;
using BeauUtil;
using FieldDay;
using FieldDay.Systems;
using System.Globalization;

namespace Pennycook {
    [SysUpdate(GameLoopPhaseMask.LateUpdate)]
    public class PenguinColliderEstimationSystem : ComponentSystemBehaviour<PenguinColliderEstimator> {
        public override void ProcessWork(float deltaTime) {
            using (new PhysicsAutoSyncScope(false)) {
                int offset = 0;
                foreach (var c in m_Components) {
                    if (!Frame.Interval(4, offset++)) {
                        continue;
                    }

                    Vector3 start, end;
                    start = c.Point0.position;
                    end = c.Point1.position;

                    Vector3 vec = end - start;
                    Vector3 center = (start + end) / 2;
                    Quaternion look = Quaternion.LookRotation(vec.normalized, Vector3.up);

                    float dist = vec.magnitude;
                    c.Collider.height = dist;
                    c.CachedTransform.SetPositionAndRotation(center, look);
                }
            }
        }

        protected override void OnComponentAdded(PenguinColliderEstimator component) {
            component.CachedTransform = component.Collider.transform;
            component.Collider.center = default;
        }
    }
}