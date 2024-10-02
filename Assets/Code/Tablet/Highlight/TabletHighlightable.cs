using System;
using BeauUtil;
using FieldDay;
using FieldDay.Components;
using FieldDay.Scripting;
using UnityEngine;

namespace Pennycook.Tablet {
    public class TabletHighlightable : BatchedComponent, IRegistrationCallbacks {
        [Header("Components")]
        public Collider HighlightCollider;

        [Inline(InlineAttribute.DisplayType.HeaderLabel)]
        public TabletHighlightContents Contents;

        [Inline(InlineAttribute.DisplayType.HeaderLabel)]
        public TabletHighlightContents UnidentifiedContents;

        [Header("State")]
        public bool Identified;

        [NonSerialized] public ScriptActor CachedActor;
        [NonSerialized] public TabletInteractable CachedInteraction;
        [NonSerialized] public TabletCapturable CachedCapture;
        [NonSerialized] public TabletWarpPoint CachedWarp;
        [NonSerialized] public TabletCountable CachedCountable;

        void IRegistrationCallbacks.OnRegister() {
            this.CacheComponent(ref CachedActor);
            this.CacheComponent(ref CachedInteraction);
            this.CacheComponent(ref CachedCapture);
            this.CacheComponent(ref CachedWarp);
            this.CacheComponent(ref CachedCountable);
        }

        void IRegistrationCallbacks.OnDeregister() {
            
        }
    }

    [Serializable]
    public struct TabletHighlightContents {
        public string ShortLabel;
        public string DetailedHeader;
        [Multiline] public string DetailedText;
    }
}