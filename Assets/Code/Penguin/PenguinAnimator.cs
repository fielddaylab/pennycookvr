using System;
using BeauUtil;
using FieldDay.Processes;
using UnityEngine;

namespace Pennycook {
    public sealed class PenguinAnimator : MonoBehaviour {
        public Animator Animator;

        [NonSerialized] public PenguinAnimFlags Flags;
        [NonSerialized] public AnimatorStateSnapshot Snapshot;
    }

    [Flags]
    public enum PenguinAnimFlags : uint {
        CannotInterrupt = 0x01,
        OnGround = 0x02,
        IsReaction = 0x04,
        IsRandomIdle = 0x08,
        AllowMove = 0x10,
    }

    static public partial class PenguinUtility {
        /// <summary>
        /// Forces the penguin to an animation state.
        /// </summary>
        static public void ForceToAnimation(PenguinAnimator animator, string state, float fadeDuration = 0, int layer = 0) {
            if (animator.Animator.GetCurrentAnimatorStateInfo(layer).IsName(state)) {
                return;
            }

            if (fadeDuration <= 0) {
                animator.Animator.Play(state, layer);
            } else {
                animator.Animator.CrossFadeInFixedTime(state, fadeDuration, layer);
            }
        }

        static public bool CanInterruptCurrentAnimState(PenguinAnimator animator) {
            return (animator.Flags & PenguinAnimFlags.CannotInterrupt) == 0;
        }
    }
}