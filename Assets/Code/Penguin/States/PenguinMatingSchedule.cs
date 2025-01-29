using System.Collections;
using UnityEngine;
using FieldDay.Processes;
using BeauUtil;
using FieldDay;

namespace Pennycook {
    public sealed class PenguinMatingSchedule : PenguinSchedule {
        public override IEnumerator Sequence(Process process) {
            PenguinBrain brain = Brain(process);
            while (true) {

                /*float newWanderRestlessness = brain.Personality.Wander.IdleWaitDuration + RNG.Instance.NextFloat(brain.Personality.Wander.IdleWaitDurationRandom);
                brain.MentalState.WanderRestlessness = newWanderRestlessness;

                while(brain.MentalState.WanderRestlessness > 0) {
                    if (brain.Contacts.GrippedShoulders.IsEmpty) {
                        brain.MentalState.WanderRestlessness -= Frame.DeltaTime;
                    }
                    yield return null;
                }*/

                Vector3 targetWalkPos;
                if(brain.Relationships != null && brain.Relationships.Mate != null) {
                    if(brain.Relationships.IsPursued) {
                        targetWalkPos = brain.transform.position;
                    } else {
                        targetWalkPos = brain.Relationships.Mate.transform.position + brain.Relationships.Mate.transform.forward * 2f;
                    }
                } else {
                    while(!TryFindGoodWanderPosition(brain, brain.Personality.Wander, brain.Type == PenguinType.Adult, out targetWalkPos)) {
                        yield return null;
                    }
                }

                while(!PenguinUtility.CanInterruptCurrentAnimState(brain.Animator)) {
                    yield return null;
                }

                if(!PenguinUtility.IsClose(brain.Relationships)) {

                    if(!brain.Relationships.IsPursued) {
                        brain.ChangeActionState(PenguinStates.Walking, new PenguinWalkParams() {
                            Target = targetWalkPos
                        });

                        yield return null;

                        while(PenguinUtility.IsNavigating(brain.Navigator)) {
                            yield return null;
                        }
                    } else {
                        
                        brain.Signal(PenguinUtility.Signals.Dancing);

                        brain.ChangeActionState(PenguinStates.Dancing, new PenguinDanceParams() {
                            Target = targetWalkPos
                        });

                        yield return 10;

                        brain.Signal(PenguinUtility.Signals.DanceComplete);
                    }
                    
                } else {

                    brain.Signal(PenguinUtility.Signals.Dancing);

                    brain.ChangeActionState(PenguinStates.Dancing, new PenguinDanceParams() {
                        Target = targetWalkPos
                    });

                    yield return 10;

                    brain.Signal(PenguinUtility.Signals.DanceComplete);
                }
            }
        }

        static private bool TryFindGoodWanderPosition(PenguinBrain root, PenguinPersonality.WanderParams wander, bool allowOutsideOfRookery, out Vector3 pos) {
            Vector3 rootPos = root.Position.position;
            Vector3 testPos = default;
            int iterations = 20;
            bool isGood = false;
            do {
                float dist = wander.WanderDistance + RNG.Instance.NextFloat(wander.WanderDistanceRandom);
                testPos = rootPos + dist * Geom.SwizzleYZ(RNG.Instance.NextVector2());
                if (allowOutsideOfRookery) {
                    isGood = PenguinNav.IsWalkable(testPos);
                } else {
                    isGood = PenguinNav.IsWalkableWithinRookery(testPos);
                }
            } while (!isGood && iterations-- > 0);

            pos = testPos;
            return isGood;
        }
    }
}