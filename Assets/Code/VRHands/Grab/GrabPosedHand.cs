using System;
using BeauUtil;
using FieldDay.Components;
using FieldDay.XR;
using UnityEngine;

namespace FieldDay.VRHands {
    public class GrabPosedHand : BatchedComponent {
        #region Inspector

        [Required] public Grabber Grabber;
        public Animator Animator;

        #endregion // Inspector

        [NonSerialized] public Transform CachedTransform;
        [NonSerialized] public XRTrackedTransform CachedTracked;

        [NonSerialized] public bool WasGripPosed;

        private void Awake() {
            this.CacheComponent(ref CachedTransform);
            this.CacheComponent(ref CachedTracked);
        }
    }
}