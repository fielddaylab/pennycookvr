using System;
using BeauUtil;
using FieldDay.Components;
using FieldDay.Scripting;
using UnityEngine;

namespace Pennycook {
    [RequireComponent(typeof(PenguinBrain))]
    public sealed class PenguinRelationshipTracker : ScriptActorComponent {
        // family
        [NonSerialized] public PenguinBrain Mate;
        [NonSerialized] public RingBuffer<PenguinBrain> Children = new RingBuffer<PenguinBrain>(4, RingBufferMode.Expand);

        // player
    }

    static public partial class PenguinUtility {
        
    }
}