using System;
using BeauUtil;
using FieldDay;
using FieldDay.Components;
using FieldDay.Scripting;
using UnityEngine;

namespace Pennycook.Tablet {
    public class TabletHighlightable : BatchedComponent, IRegistrationCallbacks {
        [Header("Components")]
        [Required] public Collider HighlightCollider;

        [Inline(InlineAttribute.DisplayType.HeaderLabel)]
        public TabletDetailsContent Contents;

        [Inline(InlineAttribute.DisplayType.HeaderLabel)]
        public TabletDetailsContent UnidentifiedContents;

        [Header("State")]
        public bool Identified;

        [NonSerialized] public ScriptActor CachedActor;
        [NonSerialized] public TabletInteractable CachedInteraction;
        [NonSerialized] public TabletCapturable CachedCapture;
        [NonSerialized] public TabletWarpPoint CachedWarp;

        void IRegistrationCallbacks.OnRegister() {
            this.CacheComponent(ref CachedActor);
            this.CacheComponent(ref CachedInteraction);
            this.CacheComponent(ref CachedCapture);
            this.CacheComponent(ref CachedWarp);
        }

        void IRegistrationCallbacks.OnDeregister() {
            
        }
    }

    [Serializable]
    public struct TabletDetailsContent {
        public string DetailedHeader;
        [Multiline] public string DetailedText;
    }
}