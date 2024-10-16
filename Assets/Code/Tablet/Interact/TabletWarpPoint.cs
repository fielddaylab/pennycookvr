using System;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Components;
using FieldDay.Scripting;
using Leaf.Runtime;
using ScriptableBake;
using UnityEngine;

namespace Pennycook.Tablet {
    [RequireComponent(typeof(TabletHighlightable))]
    public class TabletWarpPoint : BatchedComponent, IBaked {
        public enum ConnectionState {
            NotConnected,
            Connected,
            IsCurrent
        }

        public bool CanWarp = true;
        public TabletWarpPoint[] Connections;
        public TabletWarpPointGroup Group;

        [Header("Object Slots")]
        public Transform TabletCaseLocation;

        [Header("Overrides")]
        public Transform OverridePosition;
        public bool Rotate;
		
		[Header("Movement")]
		public float Radius;

        [NonSerialized] public TabletHighlightable Highlightable;
        [NonSerialized] public ConnectionState IsConnected;
        [NonSerialized] public BitSet32 AllowedConnections = new BitSet32(Bits.AllU32);

        private void Awake() {
            this.CacheComponent(ref Highlightable);
            Highlightable.HighlightCollider.enabled = false;
        }

        [LeafMember("SetWarpActive")]
        static private void LeafSetWarpActive(ScriptActor actor, bool active) {
            Assert.True(actor != null, "Null actor provided");
            TabletWarpPoint warp = actor.GetComponent<TabletWarpPoint>();
            if (warp != null) {
                warp.CanWarp = active;
                TabletWarpUtility.UpdateWarpActivation(warp);
            }
        }

        [LeafMember("SetAllWarpsActive")]
        static public void SetAllWarpsActive(bool active) {
            foreach(var warp in Find.Components<TabletWarpPoint>()) {
                warp.CanWarp = active;
                TabletWarpUtility.UpdateWarpActivation(warp);
            }
        }

        [LeafMember("SetWarpGroupActive")]
        static public void SetWarpGroupActive(TabletWarpPointGroup group, bool active) {
            foreach (var warp in Find.Components<TabletWarpPoint>()) {
                if (group == warp.Group) {
                    warp.CanWarp = active;
                    TabletWarpUtility.UpdateWarpActivation(warp);
                }
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

    public enum TabletWarpPointGroup {
        Tent,
        Bluff,
        Rookery
    }

    static public class TabletWarpUtility {
        static public void UpdateWarpActivation(TabletWarpPoint warpPoint) {
            warpPoint.Highlightable.HighlightCollider.enabled = warpPoint.CanWarp && warpPoint.IsConnected == TabletWarpPoint.ConnectionState.Connected;
        }
    }
}