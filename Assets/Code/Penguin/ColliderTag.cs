using System;
using FieldDay.Components;
using FieldDay.Physics;
using UnityEngine;

namespace Pennycook {
    public sealed class ColliderTag : BatchedComponent {
        public ColliderFlags Flags;

        static public ColliderFlags GetFlags(Collision collision, ColliderFlags fallback = 0) {
            ColliderTag tag = PhysicsExtractor.ResolveComponent<ColliderTag>(collision);
            if (tag != null) {
                return tag.Flags;
            } else {
                return fallback;
            }
        }

        static public ColliderFlags GetFlags(Collider collider, ColliderFlags fallback = 0) {
            ColliderTag tag = PhysicsExtractor.ResolveComponent<ColliderTag>(collider);
            if (tag != null) {
                return tag.Flags;
            } else {
                return fallback;
            }
        }
    }

    [Flags]
    public enum ColliderFlags : uint {
        Head        = 0x00000001,
        Torso       = 0x00000002,
        Arm         = 0x00000004,
        Leg         = 0x00000008,
        Tail        = 0x00000010,
        Beak        = 0x00000020,
        Proxy       = 0x00000040,
        
        Left        = 0x00010000,
        Right       = 0x00020000,
        Top         = 0x00040000,
        Bottom      = 0x00080000,
        Forward     = 0x00100000,
        Backward    = 0x00200000
    }
}