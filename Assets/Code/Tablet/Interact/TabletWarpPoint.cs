using System;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Components;
using FieldDay.Scripting;
using Leaf.Runtime;
using UnityEngine;

namespace Pennycook.Tablet {
    [RequireComponent(typeof(TabletHighlightable))]
    public class TabletWarpPoint : BatchedComponent {
        public bool CanWarp = true;

        [Header("Overrides")]
        public Transform OverridePosition;
        public bool Rotate;

        [LeafMember("SetWarpActive")]
        static private void LeafSetWarpActive(ScriptActor actor, bool active) {
            Assert.True(actor != null, "Null actor provided");
            TabletWarpPoint warp = actor.GetComponent<TabletWarpPoint>();
            if (warp != null) {
                warp.CanWarp = active;
            }
        }
    }
}