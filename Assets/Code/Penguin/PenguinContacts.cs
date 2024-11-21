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

        private void OnGrabbed(Grabber hand) {
            StringHash32 nodeName = GrabUtility.ResolveSnapNodeName(hand);
        }

        private void OnReleased(Grabber hand) {

        }
    }
}