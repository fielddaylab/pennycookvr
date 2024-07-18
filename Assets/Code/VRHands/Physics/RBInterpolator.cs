using FieldDay.Components;
using UnityEngine;

namespace FieldDay.Physics {
    public class RBInterpolator : BatchedComponent {
        public Rigidbody Target;

        public float VelocityMultiplier = 1;
        public float MaxVelocity = 60;
        public float AngularVelocityMultiplier = 1;
    }

    static public class RBInterpolatorUtility {
        static public void SyncInstant(RBInterpolator interpolator) {
            if (!interpolator.Target) {
                return;
            }

            interpolator.transform.GetPositionAndRotation(out Vector3 pos, out Quaternion rot);
            interpolator.Target.position = pos;
            interpolator.Target.rotation = rot;
            interpolator.Target.transform.SetPositionAndRotation(pos, rot);
            interpolator.Target.velocity = default;
            interpolator.Target.angularVelocity = default;
        }
    }
}
