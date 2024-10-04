using System;
using BeauUtil;
using UnityEngine;

namespace Pennycook.Tablet {
    public class TabletToolData {
        public readonly LayerMask RaycastMask;

        public readonly Predicate<TabletHighlightable, TabletToolState> HighlightPredicate;
        public readonly Predicate<TabletHighlightable> HighlightInteract;
    }
}