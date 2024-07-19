using System;
using FieldDay.SharedState;
using UnityEngine;

namespace Pennycook.Tablet {
    public class TabletRenderState : SharedStateComponent {
        public Transform TabletRoot;

        [Header("Screen")]
        public Material ScreenMaterial;
        public Camera ScreenCamera;
        public Transform ScreenCameraTransform;

        [Header("Screen Overlay")]
        public Camera ScreenOverlayCamera;
        public Canvas ScreenOverlayCanvas;

        [NonSerialized] public bool CurrentlyFacingScreen;
    }
}