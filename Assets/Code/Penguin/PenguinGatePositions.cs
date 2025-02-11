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
    public sealed class PenguinGatePositions : ScriptActorComponent {
        // family

        public Transform GatePosIn;

        public Transform GatePosOut;

        #region Leaf

        
        #endregion // Leaf
    }
}