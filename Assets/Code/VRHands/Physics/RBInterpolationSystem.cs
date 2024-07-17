using FieldDay;
using FieldDay.Systems;
using UnityEngine;

namespace FieldDay.Physics {
    [SysUpdate(GameLoopPhase.FixedUpdate, -100)]
    public class RBInterpolationSystem : ComponentSystemBehaviour<RBInterpolator> {
        private const float DejitterMultiplier = 0.95f;

        public override void ProcessWorkForComponent(RBInterpolator component, float deltaTime) {
            if (component.Target) {
                Transform source = component.transform;

                source.GetPositionAndRotation(out Vector3 srcPos, out Quaternion srcRot);

                Vector3 dPos = srcPos - component.Target.position;
                Vector3 dPosVec = dPos.normalized;
                float neededSpeed = Mathf.Min(dPos.magnitude / deltaTime, component.MaxVelocity);

                Vector3 neededVel = dPosVec * neededSpeed;
                component.Target.velocity = neededVel * component.VelocityMultiplier;

                Quaternion destRot = component.Target.rotation;

                Quaternion dRot = srcRot * Quaternion.Inverse(destRot);
                dRot.ToAngleAxis(out float dAngle, out Vector3 dAxis);
                if (!float.IsInfinity(dAxis.x)) {
                    if (dAngle > 180f) {
                        dAngle = 360f - dAngle;
                    }
                    component.Target.angularVelocity = dAxis.normalized * (DejitterMultiplier * dAngle * Mathf.Deg2Rad * component.AngularVelocityMultiplier / deltaTime);
                }
            }
        }

        protected override void OnComponentAdded(RBInterpolator component) {
            component.Target.maxAngularVelocity = float.MaxValue;
        }
    }
}
