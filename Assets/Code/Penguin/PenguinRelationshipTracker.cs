using System;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Components;
using FieldDay.Filters;
using FieldDay.Scripting;
using Leaf.Runtime;
using UnityEngine;

namespace Pennycook {
    [RequireComponent(typeof(PenguinBrain))]
    public sealed class PenguinRelationshipTracker : ScriptActorComponent {
        // family
        [NonSerialized] public PenguinBrain Mate;
        [NonSerialized] public RingBuffer<PenguinBrain> Children = new RingBuffer<PenguinBrain>(4, RingBufferMode.Expand);
        [NonSerialized] public AnalogSignal FamilyAnxiety;

        // player
        [NonSerialized] public AnalogSignal PlayerAnxiety;

        // social
        [NonSerialized] public AnalogSignal SocialAnxiety;

        #region Leaf

        

        #endregion // Leaf
    }

    static public partial class PenguinUtility {
        
    }
}