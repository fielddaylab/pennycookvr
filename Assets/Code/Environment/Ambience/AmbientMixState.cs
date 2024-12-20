using System;
using System.Runtime.InteropServices;
using BeauUtil;
using FieldDay.Audio;
using FieldDay.SharedState;
using UnityEngine;

namespace Pennycook {
    public sealed class AmbientMixState : SharedStateComponent {
        [Header("Outdoors")]
        [AudioBusId] public StringHash32 OutdoorBus;
        public AmbientMixGroupParams OutdoorParams;

        [Header("Indoors")]
        [AudioBusId] public StringHash32 IndoorBus;
        public AmbientMixGroupParams IndoorParams;

        [Header("References")]
        public Animator DoorAnimator;
        public Collider InteriorCollider;

        public float TransitionTime = 0.5f;

        [NonSerialized] public AmbientMixGroupState OutdoorState = AmbientMixGroupState.Uninitialized;
        [NonSerialized] public AmbientMixGroupState IndoorState = AmbientMixGroupState.Uninitialized;
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct AmbientMixGroupParams {
        public AmbientMixParams Full;
        public AmbientMixParams Partial;
        public AmbientMixParams Occcluded;
    }

    public enum AmbientMixGroupState {
        Uninitialized = -1,
        Full = 0,
        Partial,
        Occluded
    }

    [Serializable]
    public struct AmbientMixParams {
        [Range(0, 1)] public float Volume;
        [Range(0, 1)] public float LoPass;
    }
}