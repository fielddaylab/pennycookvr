using System;
using BeauUtil;
using FieldDay.Components;
using UnityEngine;

namespace FieldDay.VO {
    public class VOEmitter : BatchedComponent {
        [Required] public AudioSource Player;

        [NonSerialized] public AudioClip PlayingClip;
        [NonSerialized] public int PlayingPriority;
    }
}