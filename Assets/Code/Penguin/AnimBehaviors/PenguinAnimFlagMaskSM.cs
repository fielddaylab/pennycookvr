using BeauUtil.Debugger;
using UnityEngine;

namespace Pennycook {
    [SharedBetweenAnimators]
    public sealed class PenguinAnimFlagMaskSM : StateMachineBehaviour {
        public PenguinAnimFlags Flags;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            Assert.True(layerIndex == 0);
            animator.GetComponent<PenguinAnimator>().Flags |= Flags;
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            Assert.True(layerIndex == 0);
            animator.GetComponent<PenguinAnimator>().Flags &= ~Flags;
        }
    }
}