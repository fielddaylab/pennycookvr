using UnityEngine;
using FieldDay;
using FieldDay.Systems;
using BeauUtil;
using BeauRoutine;
using BeauUtil.Debugger;
using FieldDay.Debugging;
using System;

namespace Pennycook {
    [SysUpdate(GameLoopPhase.FixedUpdate, 100)]
    public sealed class PenguinNavigatorSystem : ComponentSystemBehaviour<PenguinNavigator> {
        public override void ProcessWorkForComponent(PenguinNavigator component, float deltaTime) {
            if (component.CurrentPath != null) {
                if ((component.Brain.Animator.Flags & PenguinAnimFlags.AllowMove) == 0) {
                    return;
                }
                HandleNavigateToNextPath(component, deltaTime);
            }
        }

        static private void HandleNavigateToNextPath(PenguinNavigator nav, float deltaTime) {
            if (!nav.CurrentPath.Positions.TryPeekFront(out Vector3 nextTarget)) {
                HandleOutOfNodes(nav);
                return;
            }

            Vector3 currentPos = nav.MoveRoot.position;

            if (nav.State == PenguinNavState.Found) {
                nav.PanicCounter = 0;
                nav.State = PenguinNavState.Moving;
                nav.CurrentPathNodeStart = currentPos;
            }

            Vector3 navStartPos = nav.CurrentPathNodeStart;
            nextTarget.y = currentPos.y;
            navStartPos.y = currentPos.y;

            DebugDraw.AddSphere(nextTarget, 0.4f, Color.green.WithAlpha(0.5f));
            DebugDraw.AddLine(nextTarget, currentPos, Color.green.WithAlpha(0.5f), 0.2f);

            Vector3 targetVector = ComputeTargetVector(nav, currentPos, navStartPos, nextTarget);
            targetVector.y = 0;

            Vector3 currentForward = nav.RotationRoot.forward;

            Vector3 flattenedCurrentForward = currentForward;
            flattenedCurrentForward.y = 0;

            Vector3 newFlatForward = Vector3.RotateTowards(flattenedCurrentForward, targetVector, deltaTime * nav.TurningSpeed * Mathf.Deg2Rad, 1);
            Vector3 newForward = newFlatForward;
            newForward.y = currentForward.y;

            nav.RotationRoot.forward = newForward;

            float angleDelta = Vector3.Angle(newFlatForward, targetVector);
            if (angleDelta < nav.MaxAngleDeltaToMove) {
                float normalizedAngleOffset = angleDelta / nav.MaxAngleDeltaToMove;
                float terrainNormal = PenguinNav.GetApproximateNormalAt(currentPos);

                float angleMultiplier = 1 - normalizedAngleOffset;
                float moveDistance = nav.MovementSpeed * deltaTime * terrainNormal * angleMultiplier;

                Vector3 newPos = currentPos + newFlatForward * moveDistance;
                if (PenguinNav.IsWalkable(newPos)) {
                    nav.PanicCounter = 0;
                    newPos = PenguinNav.SnapPositionToAccurateGround(newPos);

                    nav.MoveRoot.position = newPos;

                    float posTolerance;
                    if (nav.CurrentPath.Positions.Count > 1) {
                        posTolerance = nav.MidpointPosTolerance;
                    } else {
                        posTolerance = nav.TargetPosTolerance;
                    }

                    if (Vector2.Distance(Geom.SwizzleYZ(newPos), Geom.SwizzleYZ(nextTarget)) <= posTolerance) {
                        nav.CurrentPathNodeStart = nav.CurrentPath.Positions.PopFront();
                        if (nav.CurrentPath.Positions.Count == 0) {
                            HandleOutOfNodes(nav);
                        }
                    }
                } else {
                    DebugDraw.AddSphere(newPos, 0.3f, Color.red);
                    if (nav.PanicCounter <= 0) {
                        Log.Warn("[PenguinNavigatorSystem] Unable to move forward");
                    }
                    nav.PanicCounter += deltaTime * angleMultiplier;
                    if (nav.PanicCounter > 1f) {
                        DebugDraw.AddSphere(newPos, 1, Color.red, 3f);
                        Log.Error("[PenguinNavigatorSystem] Navigation failed");
                        HandlePanic(nav);
                    }
                }
            }
        }

        static private void HandleOutOfNodes(PenguinNavigator nav) {
            PenguinNav.FreeNavPath(ref nav.CurrentPath);
            if (nav.State != PenguinNavState.Searching) {
                nav.Brain.Signal(PenguinUtility.Signals.PathCompleted);
                VRGame.Events.Dispatch(GameEvents.PenguinReachedPathTarget, EvtArgs.Ref(nav));
                nav.State = PenguinNavState.NotPathing;
            }
        }

        static private void HandlePanic(PenguinNavigator nav) {
            PenguinNav.FreeNavPath(ref nav.CurrentPath);
            if (nav.State != PenguinNavState.Searching) {
                nav.Brain.Signal(PenguinUtility.Signals.PathNotFound);
                VRGame.Events.Dispatch(GameEvents.PenguinPathingInterrupted, EvtArgs.Ref(nav));
                nav.State = PenguinNavState.NotPathing;
            }
        }

        private const float LocalAvoidanceLookAhead = 0.6f;

        static private Vector3 ComputeTargetVector(PenguinNavigator nav, Vector3 currentPos, Vector3 targetVectorStart, Vector3 nextTarget) {
            Vector3 outputVector;
            float maxForwardDist = Vector3.Distance(nextTarget, currentPos);
            if (!ComputeTargetVectorWithWhiskers(nav, currentPos, nextTarget, maxForwardDist, out outputVector)) {
                //Vector2 closestPointAlongPath = GetClosestPointAlongVector(Geom.SwizzleYZ(targetVectorStart), Geom.SwizzleYZ(nextTarget), Geom.SwizzleYZ(currentPos));
                //Vector3 newTarget = Geom.SwizzleYZ(closestPointAlongPath);
                //newTarget.y = currentPos.y;
                //DebugDraw.AddLine(nextTarget, targetVectorStart, Color.yellow.WithAlpha(0.5f), 0.2f);
                //DebugDraw.AddPoint(newTarget, 0.3f, Color.yellow, 0.2f);
                //if (!ComputeTargetVectorWithWhiskers(nav, currentPos, newTarget, maxForwardDist, out outputVector)) {
                    outputVector = Vector3.Normalize(nextTarget - currentPos);
                //}
            }
            return outputVector;
        }

        static private bool ComputeTargetVectorWithWhiskers(PenguinNavigator nav, Vector3 currentPos, Vector3 nextTarget, float maxForwardDist, out Vector3 outputVector) {
            float travelDist = Math.Min(LocalAvoidanceLookAhead, maxForwardDist);
            Vector3 forward = Vector3.Normalize(nextTarget - currentPos);
            if (!PenguinNav.IsWalkableRaycast(currentPos, currentPos + forward * travelDist)) {
                Vector3 cross = new Vector3(-forward.z, 0, forward.x);
                Vector3 leftNav = Vector3.Normalize(forward + cross);
                if (PenguinNav.IsWalkableRaycast(currentPos, currentPos + leftNav * travelDist)) {
                    outputVector = leftNav;
                    return true;
                }
                Vector3 rightNav = Vector3.Normalize(forward - cross);
                if (PenguinNav.IsWalkableRaycast(currentPos, currentPos + rightNav * travelDist)) {
                    outputVector = rightNav;
                    return true;
                } else {
                    outputVector = forward;
                    return false;
                }
            } else {
                outputVector = forward;
                return true;
            }
        }

        static private Vector2 GetClosestPointAlongVector(Vector2 a, Vector2 b, Vector2 t) {
            Vector2 at = t - a;

            Vector2 ab = b - a;
            float abMag = ab.magnitude;
            Vector2 abDir = ab.normalized;

            float dot = Vector2.Dot(abDir, at);
            dot /= abMag;
            return a + ab * Mathf.Clamp01(dot);
        }
    }
}