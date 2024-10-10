using System;
using BeauUtil;
using FieldDay.Components;
using FieldDay.Scripting;
using ScriptableBake;
using UnityEngine;

namespace Pennycook {
    [RequireComponent(typeof(PenguinBrain))]
    public sealed class PenguinNavigator : ScriptActorComponent, IBaked {
        public Transform MoveRoot;
        public Transform RotationRoot;

        public float MovementSpeed = 2;
        public float TurningSpeed = 30;
        public float MaxAngleDeltaToMove = 60;

        [NonSerialized] public PenguinNavState State;
        [NonSerialized] public NavPath CurrentPath;
        [NonSerialized] public UniqueId16 CurrentPathRequest;
        [NonSerialized] public Transform DynamicPathTarget;
        [NonSerialized] public float PanicCounter;

        [NonSerialized] public PenguinBrain Brain;
        [NonSerialized] public float TargetPosTolerance = 0.1f;
        [NonSerialized] public float MidpointPosTolerance = 0.3f;


        private void Awake() {
            this.CacheComponent(ref Brain);
        }

#if UNITY_EDITOR

        int IBaked.Order { get { return 1000; } }

        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
            MoveRoot.position = PenguinNav.SnapPositionToAccurateGround(MoveRoot.position, LayerMasks.Terrain_Mask);
            return true;
        }

#endif // UNITY_EDITOR
    }

    public enum PenguinNavState {
        NotPathing,
        Searching,
        NotFound,
        Found,
        Moving
    }

    static public partial class PenguinUtility {
        static public partial class Signals {
            static public readonly StringHash32 PathFound = "path-found";
            static public readonly StringHash32 PathNotFound = "path-not-found";
            static public readonly StringHash32 PathCompleted = "path-completed";
        }

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
            PenguinBrain brain = nav.Brain;

            nav.CurrentPathRequest = default;
            if (path != null) {
                nav.CurrentPath = path;
                nav.State = PenguinNavState.Found;
                brain.Signal(Signals.PathFound);
            } else {
                nav.State = PenguinNavState.NotFound;
                brain.Signal(Signals.PathNotFound);
            }
        }

        static public bool IsNavigating(PenguinNavigator navigator) {
            switch (navigator.State) {
                case PenguinNavState.Searching:
                case PenguinNavState.Found:
                case PenguinNavState.Moving:
                    return true;

                default:
                    return false;
            }
        }
    }
}