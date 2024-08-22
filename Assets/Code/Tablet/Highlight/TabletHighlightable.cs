using System;
using BeauUtil;
using FieldDay;
using FieldDay.Components;
using UnityEngine;

namespace Pennycook.Tablet {
    public class TabletHighlightable : BatchedComponent, IRegistrationCallbacks {
        [Header("Components")]
        public Collider HighlightCollider;

        [Header("Contents")]
        public string ShortLabel;
        public string DetailedHeader;
        [Multiline] public string DetailedText;

        [NonSerialized] public TabletInteractable CachedInteraction;
        [NonSerialized] public TabletCapturable CachedCapture;
        [NonSerialized] public TabletWarpPoint CachedWarp;

        void IRegistrationCallbacks.OnRegister() {
            this.CacheComponent(ref CachedInteraction);
            this.CacheComponent(ref CachedCapture);
            this.CacheComponent(ref CachedWarp);
        }

        void IRegistrationCallbacks.OnDeregister() {
            
        }
    }
}