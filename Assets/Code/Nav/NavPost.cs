using System;
using BeauUtil;
using BeauUtil.Graph;
using FieldDay.Components;
using FieldDay.SharedState;
using FieldDay.Systems;
using UnityEditor;
using UnityEngine;

namespace Pennycook {
    public sealed class NavPost : BatchedComponent {
        [AutoEnum] public NavPostFlags Flags;
        public float Radius = 1;
        public NavPost[] Neighbors;

        [NonSerialized] public StringHash32 Id;
        [NonSerialized] public Vector3 Position;
        [NonSerialized] public OffsetLengthU16 Nodes;

        private void Awake() {
            Position = transform.position;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos() {
            if (!Application.isPlaying) {
                Gizmos.color = Color.yellow.WithAlpha(0.5f);
                Gizmos.DrawSphere(transform.position, Radius);
            }

            //foreach(var neighbor in Neighbors) {
            //    Gizmos.color = Color.black.WithAlpha(0.2f);
            //    Gizmos.DrawLine(transform.position, neighbor.transform.position);
            //}
        }
#endif // UNITY_EDITOR
    }

    [Flags]
    public enum NavPostFlags : uint {
        Default,
        Funnel = 0x01,
        SpawnPoint = 0x02,
        Nest = 0x04,
        OutsideRookery = 0x08,
        InsidePen = 0x10
    }
}