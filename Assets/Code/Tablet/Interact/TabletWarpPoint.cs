using System;
using BeauRoutine;
using BeauUtil;
using FieldDay.Components;
using UnityEngine;

namespace Pennycook.Tablet {
    [RequireComponent(typeof(TabletHighlightable))]
    public class TabletWarpPoint : BatchedComponent {
        public bool CanWarp = true;

        [Header("Overrides")]
        public Transform OverridePosition;
        public bool Rotate;
    }
}