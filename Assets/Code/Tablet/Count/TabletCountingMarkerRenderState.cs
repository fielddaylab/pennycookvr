using FieldDay.Animation;
using FieldDay.SharedState;
using UnityEngine;

namespace Pennycook.Tablet {
    public sealed class TabletCountingMarkerRenderState : SharedStateComponent {
        public Camera RenderCamera;
        public Mesh RenderMesh;
        public Material RenderMaterial;
        public float RenderScale;
    }
}