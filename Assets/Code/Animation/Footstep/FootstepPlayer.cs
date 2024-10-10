using System;
using BeauUtil;
using FieldDay.Audio;
using FieldDay.Components;
using Pennycook.Environment;
using UnityEngine;

namespace Pennycook.Animation {
    [RequireComponent(typeof(Animator))]
    public sealed class FootstepPlayer : BatchedComponent {
        public Transform LeftFoot;
        public Transform RightFoot;
        public FootstepDecalType DecalType;

        [AudioEventRef] public StringHash32 DefaultStep;
        [AudioEventRef] public StringHash32 SoftStep;
        [AudioEventRef] public StringHash32 LiftStep;

        [NonSerialized] public bool IsQueued;
        [NonSerialized] public FootstepIndex LastFoot;
        [NonSerialized] public FootstepType FootstepType;

        [NonSerialized] public bool Cull;
    }

    public enum FootstepIndex {
        None,
        Left,
        Right,
        Both
    }

    public enum FootstepType {
        Default,
        Soft,
        Lift
    }
}