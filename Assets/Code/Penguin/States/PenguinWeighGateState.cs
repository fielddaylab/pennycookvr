using UnityEngine;
using BeauUtil;
using FieldDay.Processes;

namespace Pennycook {
    public sealed class PenguinWeighGateState : ParameterizedPenguinState<PenguinWeighGateParams>, IProcessStateSignal {
        public void OnSignal(Process p, StringHash32 signalId, object signalArgs) {
            if (signalId == PenguinUtility.Signals.PathCompleted) {
                p.TransitionToDefault();
            } else if (signalId == PenguinUtility.Signals.PathNotFound) {
                p.TransitionToDefault();
            } else if (signalId == PenguinUtility.Signals.PathFound) {
                PenguinBrain brain = Brain(p);
                if (brain.Animator) {
                    brain.Animator.Animator.SetBool("Waddle", true);
                }
            } else if (signalId == PenguinContacts.Signal_PlayerGripped) {
                p.TransitionToDefault();
            } 
        }

        public override void OnEnter(Process p, ref PenguinWeighGateParams param) {
            PenguinBrain brain = Brain(p);
            if(!PenguinUtility.IsClose(brain.Relationships, param.Target)) {
                PenguinUtility.TryPathTo(brain.Navigator, param.Target);
            } else {
                PenguinUtility.TryPathTo(brain.Navigator, param.Target2);
            }
        }

        public override void OnExit(Process p) {
            PenguinBrain brain = Brain(p);
            if (brain.Animator) {
                brain.Animator.Animator.SetBool("Waddle", false);
            }
            PenguinUtility.StopPathing(brain.Navigator);
        }
    }

    public struct PenguinWeighGateParams {
        public Vector3 Target;
        public Vector3 Target2;
    }
}