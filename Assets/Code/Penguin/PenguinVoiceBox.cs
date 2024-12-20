using System;
using BeauUtil;
using FieldDay.Components;
using UnityEngine;

namespace Pennycook {
    [RequireComponent(typeof(AudioSource))]
    public sealed class PenguinVoiceBox : BatchedComponent {
        [Required] public Animator Animator;
        [Required] public AudioSource Audio;

        [NonSerialized] public float LastKnownVolume;
    }
}