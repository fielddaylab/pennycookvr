using System.Collections;
using UnityEngine;
using FieldDay.Processes;
using BeauUtil;

namespace Pennycook {
    public sealed class PenguinWanderSchedule : PenguinSchedule {
        public override IEnumerator Sequence(Process process) {
            PenguinBrain brain = Brain(process);
            while (true) {

                // TODO: change idle params based on player
                yield return brain.Personality.Wander.IdleWaitDuration + RNG.Instance.NextFloat(brain.Personality.Wander.IdleWaitDurationRandom);

                Vector3 targetWalkPos;
                while(!TryFindGoodWanderPosition(brain, brain.Personality.Wander, brain.Type == PenguinType.Adult, out targetWalkPos)) {
                    yield return null;
                }

                while(!PenguinUtility.CanInterruptCurrentAnimState(brain.Animator)) {
                    yield return null;
                }

                brain.ChangeActionState(PenguinStates.Walking, new PenguinWalkParams() {
                    Target = targetWalkPos
                });

                yield return null;

                while(PenguinUtility.IsNavigating(brain.Navigator)) {
                    yield return null;
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