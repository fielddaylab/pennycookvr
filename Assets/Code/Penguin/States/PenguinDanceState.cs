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
                    if(brain.Relationships.IsPursued) {
                        brain.Animator.Animator.SetBool("Bop2", true);
                    } else {
                        brain.Animator.Animator.SetBool("Bop1", true);
                        //brain.Animator.Animator.SetBool("Dance", true);
                    }
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
                    //brain.Animator.Animator.SetBool("Dance", true);
                    if(brain.Relationships.IsPursued) {
                        brain.Animator.Animator.SetBool("Bop2", true);
                    } else {
                        brain.Animator.Animator.SetBool("Bop1", true);
                        //brain.Animator.Animator.SetBool("Dance", true);
                    }
                }
            } else if(signalId == PenguinUtility.Signals.DanceComplete) {
                PenguinBrain brain = Brain(p);
                if (brain.Animator) {
                    //brain.Animator.Animator.SetBool("Dance", false);
                    if(brain.Relationships.IsPursued) {
                        brain.Animator.Animator.SetBool("Bop2", false);
                    } else {
                        brain.Animator.Animator.SetBool("Bop1", false);
                        //brain.Animator.Animator.SetBool("Dance", true);
                    }
                }
                p.TransitionToDefault();
            }
        }

        public override void OnEnter(Process p, ref PenguinDanceParams param) {
            PenguinBrain brain = Brain(p);
            if(!PenguinUtility.IsClose(brain.Relationships)) {
                PenguinUtility.TryPathTo(brain.Navigator, param.Target);
            } else {
                brain.Signal(PenguinUtility.Signals.Dancing);
            }
        }

        public override void OnExit(Process p) {
            PenguinBrain brain = Brain(p);
            if (brain.Animator) {
                //brain.Animator.Animator.SetBool("Dance", false);
                if(brain.Relationships.IsPursued) {
                    brain.Animator.Animator.SetBool("Bop2", false);
                } else {
                    brain.Animator.Animator.SetBool("Bop1", false);
                    //brain.Animator.Animator.SetBool("Dance", true);
                }
            }
            PenguinUtility.StopPathing(brain.Navigator);
        }
    }

    public struct PenguinDanceParams {
        public Vector3 Target;
    }
}