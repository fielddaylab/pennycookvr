using System;
using System.Collections.Generic;
using BeauUtil;
using FieldDay.Components;
using FieldDay.Scenes;
using Pennycook.Tablet;
using ScriptableBake;
using UnityEngine;

namespace Pennycook {
    [RequireComponent(typeof(Camera))]
    public sealed class LODReferenceCamera : BatchedComponent {
        public FixedCameraRefreshRate RefreshRate;

        [NonSerialized] public Camera CachedCamera;
        [NonSerialized] public Transform CachedTransform;

        private void Awake() {
            this.CacheComponent(ref CachedTransform);
            this.CacheComponent(ref CachedCamera);
        }
    }
}