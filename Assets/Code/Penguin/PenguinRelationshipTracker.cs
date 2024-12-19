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
        public PenguinBrain Mate;
        public PenguinBrain Child;

        #region Leaf

        

        #endregion // Leaf
    }

    static public partial class PenguinUtility {
        
    }
}