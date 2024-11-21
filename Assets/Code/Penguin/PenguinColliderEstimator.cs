using System;
using FieldDay.Components;
using UnityEngine;

namespace Pennycook {
    public sealed class PenguinColliderEstimator : BatchedComponent {
        public CapsuleCollider Collider;
        public Transform Point0;
        public Transform Point1;
        
        [NonSerialized] public Transform CachedTransform;
    }
}