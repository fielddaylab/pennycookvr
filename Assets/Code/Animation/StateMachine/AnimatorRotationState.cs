using System;
using BeauRoutine;
using BeauUtil;
using FieldDay.Animation;
using UnityEngine;

namespace Pennycook.Animation {
    [RequireComponent(typeof(Animator))]
    public sealed class AnimatorRotationState : MonoBehaviour {
        public Transform Root;

        [NonSerialized] public Vector3 StartRotation;
        [NonSerialized] public Vector3 TargetRotation;

#if UNITY_EDITOR
        private void Reset() {
            Root = transform;
        }
#endif // UNITY_EDITOR
    }
}