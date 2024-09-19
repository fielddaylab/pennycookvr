using UnityEngine;
using FieldDay;
using FieldDay.Systems;
using BeauUtil;

namespace Pennycook {
    [SysUpdate(GameLoopPhase.FixedUpdate, 100)]
    public sealed class PenguinNavigatorSystem : ComponentSystemBehaviour<PenguinNavigator> {
        public override void ProcessWorkForComponent(PenguinNavigator component, float deltaTime) {
            if (component.CurrentPath != null) {
                HandleNavigateToNextPath(component, deltaTime);
            }
        }

        static private void HandleNavigateToNextPath(PenguinNavigator nav, float deltaTime) {
            if (!nav.CurrentPath.Positions.TryPeekFront(out Vector3 nextTarget)) {
                PenguinNav.FreeNavPath(ref nav.CurrentPath);
                if (nav.State != PenguinNavState.Searching) {
                    nav.Brain.Signal("reached-path-target");
                    VRGame.Events.Dispatch(GameEvents.PenguinReachedPathTarget, EvtArgs.Ref(nav));
                    nav.State = PenguinNavState.NotPathing;
                }
                return;
            }

            if (nav.State == PenguinNavState.Found) {
                nav.State = PenguinNavState.Moving;
            }

            Vector3 currentPos = nav.MoveRoot.position;
            nextTarget.y = currentPos.y;

            Vector3 toNext = Vector3.Normalize(nextTarget - currentPos);
            Vector3 currentForward = nav.RotationRoot.forward;
        }
    }
}