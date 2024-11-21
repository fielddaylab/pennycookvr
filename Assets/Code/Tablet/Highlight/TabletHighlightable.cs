using System;
using BeauUtil;
using FieldDay;
using FieldDay.Components;
using FieldDay.Scripting;
using Leaf.Runtime;
using UnityEngine;

namespace Pennycook.Tablet {
    public class TabletHighlightable : BatchedComponent, IRegistrationCallbacks {
        [Header("Components")]
        [Required] public Collider HighlightCollider;

        [Inline(InlineAttribute.DisplayType.HeaderLabel)]
        public TabletDetailsContent Contents;

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

        #region Leaf

        [LeafMember("IsObjectIdentified")]
        static private bool LeafIsIdentified(ScriptActor actor) {
            return actor.TryGetComponent<TabletHighlightable>(out var highlight) && highlight.Identified;
        }

        #endregion // Leaf
    }

    [Serializable]
    public struct TabletDetailsContent {
        public string DetailedHeader;
        [Multiline] public string DetailedText;
    }
}