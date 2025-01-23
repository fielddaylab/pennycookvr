using UnityEngine;
using BeauUtil;
using FieldDay.Processes;

namespace Pennycook {
    public sealed class PenguinRegurgitationState : ParameterizedPenguinState<PenguinRegurgitationParams>, IProcessStateSignal {
        public void OnSignal(Process p, StringHash32 signalId, object signalArgs) {
            if (signalId == PenguinUtility.Signals.PathCompleted) {
                //p.TransitionToDefault();
                PenguinBrain brain = Brain(p);
                PenguinUtility.StopPathing(brain.Navigator);
                if (brain.Animator) {
                    brain.Animator.Animator.SetBool("Waddle", false);
                    brain.Animator.Animator.SetBool("Regurgitate", true);
                    if(brain.Relationships != null && brain.Relationships.Child != null) {
                        //Debug.Log("FEED TRUE");
                        if(brain.Relationships.Regurg != null) {
                            brain.Relationships.Regurg.Play();
                        }
                        brain.Relationships.Child.Animator.Animator.SetBool("Feed", true);
                    }
                }
            } else if (signalId == PenguinUtility.Signals.PathNotFound) {
                p.TransitionToDefault();
            } else if (signalId == PenguinUtility.Signals.PathFound) {
                PenguinBrain brain = Brain(p);
                brain.Animator.Animator.SetBool("Waddle", true);
                if(brain.Relationships != null && brain.Relationships.Child != null) {
                    brain.Relationships.Child.Animator.Animator.SetBool("Beg", true);
                }
            } else if (signalId == PenguinContacts.Signal_PlayerGripped) {
                p.TransitionToDefault();
            } else if(signalId == PenguinUtility.Signals.Regurgitating) {
                PenguinBrain brain = Brain(p);
                if (brain.Animator) {
                    brain.Animator.Animator.SetBool("Regurgitate", true);
                    if(brain.Relationships != null && brain.Relationships.Child != null) {
                        //Debug.Log("FEED TRUE 2");
                        if(brain.Relationships.Regurg != null) {
                            brain.Relationships.Regurg.Play();
                        }
                        brain.Relationships.Child.Animator.Animator.SetBool("Feed", true);
                    }
                }
            } else if(signalId == PenguinUtility.Signals.RegurgitatingComplete) {
                PenguinBrain brain = Brain(p);
                if (brain.Animator) {
                    if(brain.Relationships.Regurg != null) {
                        brain.Relationships.Regurg.Stop();
                    }
                    brain.Animator.Animator.SetBool("Regurgitate", false);
                }

                if(brain.Relationships != null && brain.Relationships.Child != null) {
                    brain.Relationships.Child.Animator.Animator.SetBool("Feed", false);
                    brain.Relationships.Child.Animator.Animator.SetBool("Beg", false);
                }

                p.TransitionToDefault();
            }
        }

        public override void OnEnter(Process p, ref PenguinRegurgitationParams param) {
            PenguinBrain brain = Brain(p);
            if(!PenguinUtility.IsClose(brain.Relationships)) {
                PenguinUtility.TryPathTo(brain.Navigator, param.Target);
            } else {
                brain.Signal(PenguinUtility.Signals.Regurgitating);
            }
        }

        public override void OnExit(Process p) {
            PenguinBrain brain = Brain(p);
            if (brain.Animator) {
                brain.Animator.Animator.SetBool("Regurgitate", false);
            }
            PenguinUtility.StopPathing(brain.Navigator);
        }
    }

    public struct PenguinRegurgitationParams {
        public Vector3 Target;
    }
}