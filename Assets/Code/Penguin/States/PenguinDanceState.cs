using UnityEngine;
using BeauUtil;
using FieldDay.Processes;

namespace Pennycook {
    public sealed class PenguinDanceState : ParameterizedPenguinState<PenguinDanceParams>, IProcessStateSignal {
        public void OnSignal(Process p, StringHash32 signalId, object signalArgs) {
            if (signalId == PenguinUtility.Signals.PathCompleted) {
                //p.TransitionToDefault();
                PenguinBrain brain = Brain(p);
                PenguinUtility.StopPathing(brain.Navigator);
                if (brain.Animator) {
                    brain.Animator.Animator.SetBool("Waddle", false);
                    brain.Animator.Animator.SetBool("Dance", true);
                    /*if(brain.Relationships != null && brain.Relationships.Mate != null && brain.Relationships.IsPursued) {
                        brain.Relationships.Mate.Animator.Animator.SetBool("Dance", true);
                        brain.Relationships.Mate.Relationships.IsPursued = true;
                    }*/
                }
            } else if (signalId == PenguinUtility.Signals.PathNotFound) {
                p.TransitionToDefault();
            } else if (signalId == PenguinUtility.Signals.PathFound) {
                PenguinBrain brain = Brain(p);
                if(!brain.Relationships.IsPursued) {
                    brain.Animator.Animator.SetBool("Waddle", true);
                }
            } else if (signalId == PenguinContacts.Signal_PlayerGripped) {
                p.TransitionToDefault();
            } else if(signalId == PenguinUtility.Signals.Dancing) {
                PenguinBrain brain = Brain(p);
                if (brain.Animator) {
                    brain.Animator.Animator.SetBool("Dance", true);
                }
            } else if(signalId == PenguinUtility.Signals.DanceComplete) {
                PenguinBrain brain = Brain(p);
                if (brain.Animator) {
                    brain.Animator.Animator.SetBool("Dance", false);
                    /*if(brain.Relationships != null && brain.Relationships.Mate != null && brain.Relationships.IsPursued) {
                        brain.Relationships.Mate.Animator.Animator.SetBool("Dance", false);
                        brain.Relationships.Mate.Relationships.IsPursued = false;
                    }*/
                }
                p.TransitionToDefault();
            }
        }

        public override void OnEnter(Process p, ref PenguinDanceParams param) {
            PenguinBrain brain = Brain(p);
            PenguinUtility.TryPathTo(brain.Navigator, param.Target);
        }

        public override void OnExit(Process p) {
            PenguinBrain brain = Brain(p);
            if (brain.Animator) {
                brain.Animator.Animator.SetBool("Dance", false);
                if(brain.Relationships != null && brain.Relationships.Mate != null && brain.Relationships.IsPursued) {
                    brain.Relationships.Mate.Animator.Animator.SetBool("Dance", false);
                }
            }
            PenguinUtility.StopPathing(brain.Navigator);
        }
    }

    public struct PenguinDanceParams {
        public Vector3 Target;
    }
}