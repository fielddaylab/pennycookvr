using System;
using BeauUtil;
using FieldDay.Components;
using FieldDay.Scripting;
using FieldDay.VRHands;
using UnityEngine;

namespace Pennycook {
    [RequireComponent(typeof(PenguinBrain))]
    public class PenguinContacts : ScriptActorComponent {
        static public readonly StringHash32 Signal_PlayerGripped = "penguin-gripped";
        static public readonly StringHash32 Signal_PlayerReleased = "penguin-released";

        public TriggerListener Proximity;
        public CollisionListener Collisions;
        public Grabbable PlayerGrip;

        [NonSerialized] public PenguinBrain CachedBrain;
        [NonSerialized] public BitSet32 GrippedShoulders;

        private void Awake() {
            this.CacheComponent(ref CachedBrain);

            PlayerGrip.OnGrabbed.Register(OnGrabbed);
            PlayerGrip.OnReleased.Register(OnReleased);
        }

        private void OnDestroy() {
            PlayerGrip.OnGrabbed.Deregister(OnGrabbed);
            PlayerGrip.OnReleased.Deregister(OnReleased);
        }

        private void OnGrabbed(Grabber hand, int snapIndex) {
            var node = GrabUtility.ResolveSnapNode(PlayerGrip, snapIndex);
            if (node.Label == "LShoulder") {
                GrippedShoulders.Set(0);
            } else if (node.Label == "RShoulder") {
                GrippedShoulders.Set(1);
            }

            if (GrippedShoulders.Count == 1) {
                CachedBrain.Signal(Signal_PlayerGripped);
            }
        }

        private void OnReleased(Grabber hand, int snapIndex) {
            var node = GrabUtility.ResolveSnapNode(PlayerGrip, snapIndex);
            if (node.Label == "LShoulder") {
                GrippedShoulders.Unset(0);
            } else if (node.Label == "RShoulder") {
                GrippedShoulders.Unset(1);
            }

            if (GrippedShoulders.Count == 1) {
                CachedBrain.Signal(Signal_PlayerReleased);
            }
        }
    }
}