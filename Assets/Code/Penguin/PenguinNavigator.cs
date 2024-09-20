using System;
using BeauUtil;
using FieldDay.Components;
using FieldDay.Scripting;
using UnityEngine;

namespace Pennycook {
    [RequireComponent(typeof(PenguinBrain))]
    public sealed class PenguinNavigator : ScriptActorComponent {
        public Transform MoveRoot;
        public Transform RotationRoot;

        public float MovementSpeed = 2;
        public float TurningSpeed = 30;
        public float MaxAngleDeltaToMove = 60;

        [NonSerialized] public PenguinNavState State;
        [NonSerialized] public NavPath CurrentPath;
        [NonSerialized] public UniqueId16 CurrentPathRequest;
        [NonSerialized] public Transform DynamicPathTarget;

        [NonSerialized] public PenguinBrain Brain;
        [NonSerialized] public float TargetPosTolerance = 0.3f;

        private void Awake() {
            this.CacheComponent(ref Brain);
        }
    }

    public enum PenguinNavState {
        NotPathing,
        Searching,
        NotFound,
        Found,
        Moving
    }

    static public partial class PenguinUtility {
        /// <summary>
        /// Attempts to path a navigator towards a point.
        /// </summary>
        static public void TryPathTo(PenguinNavigator navigator, Vector3 position) {
            navigator.State = PenguinNavState.Searching;
            PenguinNav.CancelPath(ref navigator.CurrentPathRequest);
            PenguinNav.FreeNavPath(ref navigator.CurrentPath);
            navigator.CurrentPathRequest = PenguinNav.RequestPath(navigator.MoveRoot.position, position, OnPathResponse, navigator);
        }

        /// <summary>
        /// Stops pathing for 
        /// </summary>
        static public void StopPathing(PenguinNavigator navigator) {
            navigator.State = PenguinNavState.NotPathing;
            PenguinNav.CancelPath(ref navigator.CurrentPathRequest);
            PenguinNav.FreeNavPath(ref navigator.CurrentPath);
        }

        static private readonly PenguinPathResponseHandler OnPathResponse = OnPathFindResponse;

        static private void OnPathFindResponse(NavPath path, object context) {
            PenguinNavigator nav = (PenguinNavigator) context;

            nav.CurrentPathRequest = default;
            if (path != null) {
                nav.CurrentPath = path;
                nav.State = PenguinNavState.Found;
            } else {
                nav.State = PenguinNavState.NotFound;
            }
        }
    }
}