using System;
using BeauUtil;
using FieldDay.Components;
using UnityEngine;

namespace Pennycook.Tablet {
    [RequireComponent(typeof(TabletHighlightable))]
    public sealed class TabletCountable : BatchedComponent {
        public TabletCountingGroup Group;

        
    }

    static public class TabletCountUtility {
        static public bool IsCountable(TabletCountable countable) {
            var group = countable.Group;
            if (group.State == TabletCountingGroupState.Inactive || group.State == TabletCountingGroupState.Completed) {
                return false;
            }
            if (group.CurrentlyCounted.Contains(countable)) {
                return false;
            }
            return true;
        }
    }
}