using BeauUtil;
using FieldDay.Animation;
using UnityEngine;

namespace Pennycook.Animation {
    [SharedBetweenAnimators]
    public sealed class AdditiveBlendMaskSM : FrameKeyedSMBehaviour {
        [Range(0, 7)] public int LayerIndex;
        [Range(0, 1)] public float DefaultWeight;
        [Range(0, 1)] public float InRangeWeight;
        public OffsetLengthU16[] Ranges;

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            int frame = CurrentFrame(stateInfo);

            AdditiveBlendMaskState blendMask = animator.GetComponent<AdditiveBlendMaskState>();
            float layerWeight = AnimUtility.FrameInRange((ushort) frame, Ranges) ? InRangeWeight : DefaultWeight;
            blendMask.StateMachineWeights.Target[LayerIndex] = layerWeight;
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            AdditiveBlendMaskState blendMask = animator.GetComponent<AdditiveBlendMaskState>();
            blendMask.StateMachineWeights.Target[LayerIndex] = DefaultWeight;
        }
    }
}