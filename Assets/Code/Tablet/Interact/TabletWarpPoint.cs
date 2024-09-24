using System;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Components;
using FieldDay.Scripting;
using Leaf.Runtime;
using ScriptableBake;
using UnityEngine;

namespace Pennycook.Tablet {
    [RequireComponent(typeof(TabletHighlightable))]
    public class TabletWarpPoint : BatchedComponent, IBaked {
        public bool CanWarp = true;
        public TabletWarpPoint[] Connections;

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

#if UNITY_EDITOR

        int IBaked.Order { get { return 1000; } }

        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
            int idx = ArrayUtils.IndexOf(Connections, this);
            if (idx >= 0) {
                ArrayUtils.RemoveAt(ref Connections, idx);
                return true;
            }

            return false;
        }

#endif // UNITY_EDITOR
    }
}