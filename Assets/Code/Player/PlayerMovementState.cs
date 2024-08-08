using System;
using System.Collections;
using BeauRoutine;
using FieldDay;
using FieldDay.SharedState;
using Pennycook.Tablet;
using UnityEngine;

namespace Pennycook {
    public class PlayerMovementState : SharedStateComponent {
        public enum State {
            Default,
            Warping
        }

        [Header("Components")]
        public SpriteRenderer WarpFader;

        [NonSerialized] public State CurrentState;
        public Routine WarpRoutine;
    }

    static public class PlayerMovementUtility {
        static public void WarpTo(PlayerMovementState state, TabletWarpPoint warpPoint) {
            if (state.WarpRoutine || state.CurrentState == PlayerMovementState.State.Warping) {
                return;
            }

            state.CurrentState = PlayerMovementState.State.Warping;
            state.WarpRoutine.Replace(state, WarpRoutine(state, warpPoint));
        }

        static private IEnumerator WarpRoutine(PlayerMovementState state, TabletWarpPoint warpPoint) {
            state.WarpFader.enabled = true;
            yield return state.WarpFader.FadeTo(1, 0.4f);
            yield return 0.1f;
            PlayerRig rig = Find.State<PlayerRig>();
            using (var move = new PlayerRigUtils.MovementRequest(rig)) {
                Transform location = warpPoint.OverridePosition ? warpPoint.OverridePosition : warpPoint.transform;
                if (warpPoint.Rotate) {
                    move.Teleport(location.position, location.forward);
                } else {
                    move.Teleport(location.position);
                }
            }
            yield return 0.1f;
            yield return state.WarpFader.FadeTo(0, 0.4f);
            state.WarpFader.enabled = false;
            state.CurrentState = PlayerMovementState.State.Default;
        }
    }
}