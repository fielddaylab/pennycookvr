using System;
using FieldDay.Components;
using UnityEngine;

namespace Pennycook.Animation {
	public sealed class LookSmoothing : BatchedComponent {
        public Animator Animator;
        public Transform LookFrom;
        public float LookLerpSpeed;

        [NonSerialized] public LookTargetMode Mode;
        [NonSerialized] public Vector3 LookVector;
        [NonSerialized] public Transform LookTowards;
        [NonSerialized] public Vector2 LastAppliedLook;

        public Vector2 WorldLookDirectionToLocal(Vector3 worldVec) {
            return (Vector2) LookFrom.InverseTransformDirection(worldVec);
        }
    }

    public enum LookTargetMode {
        Disabled,
        Forward,
        ConstantLocal,
        ConstantWorld,
        TowardsTransform
    }
}