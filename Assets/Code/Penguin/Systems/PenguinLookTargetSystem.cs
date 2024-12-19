using BeauUtil;
using FieldDay;
using FieldDay.Systems;
using Pennycook.Animation;
using UnityEngine;

namespace Pennycook {
    [SysUpdate(GameLoopPhase.Update, -10)]
    public sealed class PenguinLookTargetSystem : ComponentSystemBehaviour<PenguinBrain, PenguinContacts, LookSmoothing> {
        public override void ProcessWork(float deltaTime) {
            PlayerRig playerRig = Find.State<PlayerRig>();

            for(int i = 0; i < m_Components.Count; i++) {
                if (!Frame.Interval(3, i)) {
                    continue;
                }

                var components = m_Components[i];
                Transform lookTarget = null;
                if (!components.ComponentA.GrippedShoulders.IsEmpty) {
                    lookTarget = playerRig.HeadLook;
                } else if ((components.ComponentA.PlayerProximity.Object != null)) {
                    lookTarget = GetClosestPlayerLookTarget(components.ComponentB, playerRig);
                } else {
                    lookTarget = null;
                }

                components.ComponentB.LookTowards = lookTarget;
                if (lookTarget) {
                    components.ComponentB.Mode = LookTargetMode.TowardsTransform;
                } else {
                    components.ComponentB.Mode = LookTargetMode.Forward;
                }
            }
        }

        static private unsafe Transform GetClosestPlayerLookTarget(LookSmoothing looker, PlayerRig player) {
            Vector3* playerTargets = stackalloc Vector3[3];
            playerTargets[0] = player.HeadLook.position;
            playerTargets[1] = player.LeftHand.Grabber.CachedTransform.position;
            playerTargets[2] = player.RightHand.Grabber.CachedTransform.position;

            int closestPos = GetClosestPosition(looker.LookFrom.position, new UnsafeSpan<Vector3>(playerTargets, 3), out var _);
            switch (closestPos) {
                case 0:
                default: {
                    return player.HeadLook;
                }
                case 1: {
                    return player.LeftHand.Grabber.CachedTransform;
                }
                case 2: {
                    return player.RightHand.Grabber.CachedTransform;
                }
            }
        }

        static private unsafe int GetClosestPosition(Vector3 rootPos, UnsafeSpan<Vector3> positions, out Vector3 pos) {
            if (positions.Length <= 0) {
                pos = default;
                return -1;
            }

            float minDistSq = Vector3.SqrMagnitude(positions[0] - rootPos);
            int minDistIdx = 0;

            for(int i = 1; i < positions.Length; i++) {
                float newDistSq = Vector3.SqrMagnitude(positions[i] - rootPos);
                if (newDistSq < minDistSq) {
                    minDistSq = newDistSq;
                    minDistIdx = i;
                }
            }

            pos = positions[minDistIdx];
            return minDistIdx;
        }
    }
}