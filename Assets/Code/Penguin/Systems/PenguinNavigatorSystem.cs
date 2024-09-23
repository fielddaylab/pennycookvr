using UnityEngine;
using FieldDay;
using FieldDay.Systems;
using BeauUtil;
using BeauRoutine;
using BeauUtil.Debugger;
using FieldDay.Debugging;

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

            if (nav.State == PenguinNavState.Found) {
                nav.PanicCounter = 0;
                nav.State = PenguinNavState.Moving;
            }

            Vector3 currentPos = nav.MoveRoot.position;
            nextTarget.y = currentPos.y;

            DebugDraw.AddSphere(nextTarget, 0.4f, Color.green.WithAlpha(0.5f));
            DebugDraw.AddLine(nextTarget, currentPos, Color.green.WithAlpha(0.5f), 0.2f);

            Vector3 targetVector = ComputeTargetVector(nav, currentPos, nextTarget);
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

                float angleMultiplier = Curve.CubeInOut.Evaluate(1 - normalizedAngleOffset);
                float moveDistance = nav.MovementSpeed * deltaTime * (terrainNormal * terrainNormal) * angleMultiplier;

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
                        nav.CurrentPath.Positions.PopFront();
                        if (nav.CurrentPath.Positions.Count == 0) {
                            HandleOutOfNodes(nav);
                        }
                    }
                } else {
                    DebugDraw.AddSphere(newPos, 0.3f, Color.red);
                    nav.PanicCounter += deltaTime;
                    Log.Warn("[PenguinNavigatorSystem] Unable to move forward");
                    if (nav.PanicCounter > 1f) {
                        DebugDraw.AddSphere(newPos, 1, Color.red, 3f);
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

        private const float LocalAvoidanceLookAhead = 0.5f;

        static private Vector3 ComputeTargetVector(PenguinNavigator nav, Vector3 currentPos, Vector3 nextTarget) {
            Vector3 forward = Vector3.Normalize(nextTarget - currentPos);
            if (!PenguinNav.IsWalkable(currentPos + forward * LocalAvoidanceLookAhead)) {
                Vector3 cross = new Vector3(-forward.z, 0, forward.x);
                Vector3 leftNav = Vector3.Normalize(forward + cross);
                if (PenguinNav.IsWalkable(currentPos + leftNav * LocalAvoidanceLookAhead)) {
                    return leftNav;
                }
                Vector3 rightNav = Vector3.Normalize(forward - cross);
                if (PenguinNav.IsWalkable(currentPos + rightNav * LocalAvoidanceLookAhead)) {
                    return rightNav;
                } else {
                    return forward;
                }
            } else {
                return forward;
            }
        }
    }
}