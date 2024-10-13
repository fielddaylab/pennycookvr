using BeauRoutine;
using BeauUtil;
using FieldDay.Animation;
using UnityEngine;

namespace Pennycook.Animation {
    [SharedBetweenAnimators]
    public sealed class RotateAroundSM : FrameKeyedSMBehaviour {
        public OffsetLengthU16 FrameRange;
        public Vector3 RotateAmount;
        public Curve RotateCurve;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            AnimatorRotationState rotState = animator.GetComponent<AnimatorRotationState>();
            if (!rotState) {
                rotState = animator.gameObject.AddComponent<AnimatorRotationState>();
            }
            rotState.StartRotation = rotState.CacheComponent(ref rotState.Root).localEulerAngles;
            rotState.TargetRotation = rotState.StartRotation + RotateAmount;
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            AnimatorRotationState rotState = animator.GetComponent<AnimatorRotationState>();
            int frame = CurrentFrame(stateInfo);
            
            Vector3 desiredRot;
            if (frame < FrameRange.Offset) {
                return;
            }
            
            if (frame >= FrameRange.End) {
                desiredRot = rotState.TargetRotation;
            } else {
                float lerp = (frame - FrameRange.Offset) / (float) FrameRange.Length;
                desiredRot = Vector3.Lerp(rotState.StartRotation, rotState.TargetRotation, RotateCurve.Evaluate(lerp));
            }

            rotState.Root.localEulerAngles = desiredRot;
        }
    }
}