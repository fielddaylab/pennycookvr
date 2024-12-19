using System;
using BeauUtil;
using FieldDay.Components;
using FieldDay.Physics;
using FieldDay.Scripting;
using FieldDay.VRHands;
using UnityEngine;

namespace Pennycook {
    [RequireComponent(typeof(PenguinBrain))]
    public class PenguinContacts : ScriptActorComponent {
        static public readonly StringHash32 Signal_PlayerGripped = "penguin-gripped";
        static public readonly StringHash32 Signal_PlayerReleased = "penguin-released";
        static public readonly StringHash32 Signal_PlayerEnteredProximity = "penguin-player-in-proximity";
        static public readonly StringHash32 Signal_PlayerExitedProximity = "penguin-player-exited-proximity";

        public TriggerListener Proximity;
        public CollisionListener Collisions;
        public Grabbable PlayerGrip;

        [NonSerialized] public PenguinBrain CachedBrain;
        [NonSerialized] public BitSet32 GrippedShoulders;
        [NonSerialized] public SensedObject<PlayerRig> PlayerProximity;

        private void Awake() {
            this.CacheComponent(ref CachedBrain);

            PlayerGrip.OnGrabbed.Register(OnGrabbed);
            PlayerGrip.OnReleased.Register(OnReleased);

            if (Proximity) {
                Proximity.onTriggerEnter.Register(OnProximityEnter);
                Proximity.onTriggerExit.Register(OnProximityExit);
            }
        }

        private void OnDestroy() {
            PlayerGrip.OnGrabbed.Deregister(OnGrabbed);
            PlayerGrip.OnReleased.Deregister(OnReleased);

            if (Proximity) {
                Proximity.onTriggerEnter.Deregister(OnProximityEnter);
                Proximity.onTriggerExit.Deregister(OnProximityExit);
            }
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
                CachedBrain.Animator.Animator.SetBool("PlayerGripping", true);
            }
        }

        private void OnReleased(Grabber hand, int snapIndex) {
            var node = GrabUtility.ResolveSnapNode(PlayerGrip, snapIndex);
            if (node.Label == "LShoulder") {
                GrippedShoulders.Unset(0);
            } else if (node.Label == "RShoulder") {
                GrippedShoulders.Unset(1);
            }

            if (GrippedShoulders.Count == 0) {
                CachedBrain.Signal(Signal_PlayerReleased);
                CachedBrain.Animator.Animator.SetBool("PlayerGripping", false);
            }
        }
    
        private void OnProximityEnter(Collider collider) {
            if (collider.gameObject.layer == LayerMasks.PenguinBody_Index) {
                // was penguin
            } else {
                PlayerRig player = collider.ResolveComponent<PlayerRig>();
                if (player != null) {
                    if (SenseUtility.Increment(ref PlayerProximity, player)) {
                        CachedBrain.Signal(Signal_PlayerEnteredProximity);
                    }
                }
            }
        }

        private void OnProximityExit(Collider collider) {
            if (!collider) {
                return;
            }

            if (collider.gameObject.layer == LayerMasks.PenguinBody_Index) {
                // was penguin
            } else {
                PlayerRig player = collider.ResolveComponent<PlayerRig>();
                if (player != null) {
                    if (SenseUtility.Decrement(ref PlayerProximity, player)) {
                        CachedBrain.Signal(Signal_PlayerEnteredProximity);
                    }
                }
            }
        }
    }

    static public class SenseUtility {
        static public bool Increment<T>(ref SensedObject<T> sense, T obj) where T : class {
            if (sense.Object != obj) {
                sense.Object = obj;
                sense.Colliders = 0;
            }
            return (sense.Colliders++ == 0);
        }

        static public bool Decrement<T>(ref SensedObject<T> sense, T obj) where T : class {
            if (sense.Object == obj) {
                if (sense.Colliders-- == 1) {
                    sense.Object = null;
                    return true;
                }
            }
            return false;
        }
    }

    public struct SensedObject<T> where T : class {
        public T Object;
        public int Colliders;

        static public implicit operator T(SensedObject<T> sensed) {
            return sensed.Object;
        }
    }

    public struct SensedObjectCollection<T> where T : class {
        
    }
}