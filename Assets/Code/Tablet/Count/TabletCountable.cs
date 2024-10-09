using BeauUtil;
using FieldDay.Components;
using UnityEngine;

namespace Pennycook.Tablet {
    [RequireComponent(typeof(TabletHighlightable))]
    public sealed class TabletCountable : BatchedComponent {
        public TabletCountingGroup Group;
    }
}