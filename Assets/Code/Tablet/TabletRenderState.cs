using FieldDay.SharedState;
using UnityEngine;

namespace Pennycook {
    public class TabletRenderState : SharedStateComponent {
        public Transform TabletRoot;

        [Header("Screen")]
        public Material ScreenMaterial;
        public Camera ScreenCamera;

        [Header("Screen Overlay")]
        public Canvas ScreenOverlayCanvas;
    }
}