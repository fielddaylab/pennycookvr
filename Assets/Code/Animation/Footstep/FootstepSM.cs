using BeauUtil;
using FieldDay.Animation;
using UnityEngine;

namespace Pennycook.Animation {
    [SharedBetweenAnimators]
    public sealed class FootstepSM : FrameKeyedSMBehaviour {
        [SerializeField] private FootstepType m_StepType;

        [Header("Alt Footstep Type")]
        [SerializeField] private FootstepType m_AltFootstepType;
        [SerializeField] private OffsetLengthU16[] m_AltFootstepRange;

        [Header("Frames")]
        [SerializeField] private OffsetLengthU16[] m_LeftSteps;
        [SerializeField] private OffsetLengthU16[] m_RightSteps;

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            FootstepPlayer player = animator.GetComponent<FootstepPlayer>();
            if (player.Cull) {
                return;
            }

            ushort frame = (ushort) CurrentFrame(stateInfo);

            bool isLeft = AnimUtility.FrameInRange(frame, m_LeftSteps);
            bool isRight = AnimUtility.FrameInRange(frame, m_RightSteps);

            if (m_AltFootstepRange.Length > 0 && AnimUtility.FrameInRange(frame, m_AltFootstepRange)) {
                player.FootstepType = m_AltFootstepType;
            } else {
                player.FootstepType = m_StepType;
            }

            if (isLeft && isRight) {
                if (player.LastFoot != FootstepIndex.Both) {
                    player.LastFoot = FootstepIndex.Both;
                    player.IsQueued = true;
                }
            } else if (isLeft) {
                if (player.LastFoot != FootstepIndex.Left) {
                    player.LastFoot = FootstepIndex.Left;
                    player.IsQueued = true;
                }
            } else if (isRight) {
                if (player.LastFoot != FootstepIndex.Right) {
                    player.LastFoot = FootstepIndex.Right;
                    player.IsQueued = true;
                }
            }
        }
    }
}