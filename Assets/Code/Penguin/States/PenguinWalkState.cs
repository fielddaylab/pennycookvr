using UnityEngine;
using BeauUtil;
using FieldDay.Processes;

namespace Pennycook {
    public sealed class PenguinWalkState : ParameterizedPenguinState<PenguinWalkParams>, IProcessStateSignal {
        public void OnSignal(Process p, StringHash32 signalId, object signalArgs) {
            if (signalId == PenguinUtility.Signals.PathCompleted) {
                p.TransitionToDefault();
            } else if (signalId == PenguinUtility.Signals.PathNotFound) {
                p.TransitionToDefault();
            } else if (signalId == PenguinUtility.Signals.PathFound) {
                PenguinBrain brain = Brain(p);
                brain.Animator.Animator.SetBool("Waddle", true);
            }
        }

        public override void OnEnter(Process p, ref PenguinWalkParams param) {
            PenguinBrain brain = Brain(p);
            PenguinUtility.TryPathTo(brain.Navigator, param.Target);
        }

        public override void OnExit(Process p) {
            PenguinBrain brain = Brain(p);
            brain.Animator.Animator.SetBool("Waddle", false);
            PenguinUtility.StopPathing(brain.Navigator);
        }
    }

    public struct PenguinWalkParams {
        public Vector3 Target;
    }
}