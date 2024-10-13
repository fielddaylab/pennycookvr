using System;
using System.Collections;
using System.Collections.Generic;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Scenes;
using FieldDay.Scripting;
using FieldDay.SharedState;
using Leaf.Runtime;
using Pennycook.Tablet;
using UnityEngine;
using UnityEngine.UI;

namespace Pennycook {
    public class PlayerMovementState : SharedStateComponent {
        public enum State {
            Default,
            Warping
        }

        [Header("Components")]
        public SpriteRenderer WarpFader;

        [NonSerialized] public State CurrentState;
        [NonSerialized] public TabletWarpPoint CurrentWarp;
        public Routine WarpRoutine;
    }

    static public class PlayerMovementUtility {
        static public bool WarpTo(PlayerMovementState state, TabletWarpPoint warpPoint, bool interruptCurrentWarp = false) {
            if (!interruptCurrentWarp && (state.WarpRoutine || state.CurrentState == PlayerMovementState.State.Warping)) {
                return false;
            }

            state.CurrentState = PlayerMovementState.State.Warping;
            state.WarpRoutine.Replace(state, WarpRoutine(state, warpPoint, warpPoint.Rotate));
            return true;
        }

        static private IEnumerator WarpRoutine(PlayerMovementState state, TabletWarpPoint warpPoint, bool rotate) {
            state.WarpFader.enabled = true;
            yield return state.WarpFader.FadeTo(1, 0.4f);
            yield return 0.1f;
            PlayerRig rig = Find.State<PlayerRig>();
            using (var move = new PlayerRigUtils.MovementRequest(rig)) {
                Transform location = warpPoint.OverridePosition ? warpPoint.OverridePosition : warpPoint.transform;
                if (rotate) {
                    move.Teleport(location.position, location.forward);
                } else {
                    move.Teleport(location.position);
                }
            }
            yield return 0.1f;
            SetCurrentWarp(state, warpPoint);
            using (var t = TempVarTable.Alloc()) {
                t.Set("targetId", ScriptUtility.ActorId(warpPoint));
                t.Set("targetObject", warpPoint.name);
                ScriptUtility.Trigger(GameTriggers.AtWarpPoint, t);
            }
            yield return state.WarpFader.FadeTo(0, 0.4f);
            state.WarpFader.enabled = false;
            state.CurrentState = PlayerMovementState.State.Default;
        }

        static private void InstantWarp(PlayerMovementState state, TabletWarpPoint warpPoint, bool rotate, bool sendTriggers) {
            state.CurrentState = PlayerMovementState.State.Default;
            state.WarpFader.enabled = false;
            state.WarpFader.SetAlpha(0);
            state.WarpRoutine.Stop();

            SetCurrentWarp(state, warpPoint, true);

            PlayerRig rig = Find.State<PlayerRig>();
            using (var move = new PlayerRigUtils.MovementRequest(rig)) {
                Transform location = warpPoint.OverridePosition ? warpPoint.OverridePosition : warpPoint.transform;
                if (rotate) {
                    move.Teleport(location.position, location.forward);
                } else {
                    move.Teleport(location.position);
                }
            }

            if (sendTriggers) {
                using (var t = TempVarTable.Alloc()) {
                    t.Set("targetId", ScriptUtility.ActorId(warpPoint));
                    t.Set("targetObject", warpPoint.name);
                    ScriptUtility.Trigger(GameTriggers.AtWarpPoint, t);
                }
            }
        }

        static public void SetCurrentWarp(PlayerMovementState state, TabletWarpPoint warpPoint, bool force = false) {
            TabletWarpPoint current = state.CurrentWarp;
            if (!force && current == warpPoint) {
                return;
            }

            using(PooledSet<TabletWarpPoint> affectedPoints = PooledSet<TabletWarpPoint>.Create()) {
                if (current != null) {
                    foreach (var connection in current.Connections) {
                        connection.IsConnected = TabletWarpPoint.ConnectionState.NotConnected;
                        affectedPoints.Add(connection);
                    }
                    current.IsConnected = TabletWarpPoint.ConnectionState.NotConnected;
                    affectedPoints.Add(current);
                }

                Log.Msg("[PlayerMovementUtility] Warp changing from '{0}' to '{1}'", state.CurrentWarp, warpPoint);
                state.CurrentWarp = warpPoint;

                if (warpPoint != null) {
                    foreach (var connection in warpPoint.Connections) {
                        connection.IsConnected = TabletWarpPoint.ConnectionState.Connected;
                        affectedPoints.Add(connection);
                    }

                    warpPoint.IsConnected = TabletWarpPoint.ConnectionState.IsCurrent;
                    affectedPoints.Add(warpPoint);
                }

                foreach(var affected in affectedPoints) {
                    TabletWarpUtility.UpdateWarpActivation(affected);
                }
            }

            VRGame.Events.Dispatch(GameEvents.WarpPointUpdated, EvtArgs.Ref(warpPoint));
        }

        [LeafMember("PlacePlayerAt")]
        static private void LeafPlacePlayerAt(ScriptActor target) {
            LeafPlaceAndRotatePlayerAt(target, false);
        }

        [LeafMember("PlaceAndRotatePlayerAt")]
        static private void LeafPlaceAndRotatePlayerAt(ScriptActor target, bool rotate) {
            if (target == null) {
                Log.Error("[PlacePlayerAt] No actor provided");
                return;
            }

            if (!target.TryGetComponent(out TabletWarpPoint warpPoint)) {
                Log.Error("[PlacePlayerAt] Actor '{0}' has no warp points attached", target.Id);
                return;
            }

            InstantWarp(Find.State<PlayerMovementState>(), warpPoint, warpPoint.Rotate || rotate, Game.Scenes.IsMainLoaded());
        }

        [LeafMember("WarpPlayerTo")]
        static private void LeafWarpPlayerTo(ScriptActor target) {
            if (target == null) {
                Log.Error("[WarpPlayerTo] No actor provided");
                return;
            }

            if (!target.TryGetComponent(out TabletWarpPoint warpPoint)) {
                Log.Error("[WarpPlayerTo] Actor '{0}' has no warp points attached", target.Id);
                return;
            }

            var movementState = Find.State<PlayerMovementState>();
            if (movementState.CurrentWarp == warpPoint) {
                Log.Msg("[WarpPlayerTo] Player is already at target node '{0}'", target.Id);
                return;
            }

            WarpTo(movementState, warpPoint, true);
        }
    }
}