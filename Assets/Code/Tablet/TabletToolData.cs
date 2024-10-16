using System;
using BeauUtil;
using FieldDay;
using FieldDay.HID.XR;
using FieldDay.XR;
using UnityEngine;

namespace Pennycook.Tablet {
    public class TabletToolDefinition {
        public LayerMask RaycastMask;

        public Predicate<TabletHighlightable, TabletHighlightState> HighlightPredicate;

        public TabletToolAnalyzePredicate GetState;
        
        public string DefaultVerb;
        public TabletToolInteractionVerbPredicate GetVerb;

        public TabletToolUpdateAction Update;

        public TabletToolInteractionMode InteractMode;
        public TabletToolInteractDelegate Interact;
    }

    public delegate TabletInteractionState.State TabletToolAnalyzePredicate(TabletHighlightable highlightable, TabletControlState controlState, double timestamp);
    public delegate string TabletToolInteractionVerbPredicate(TabletHighlightable highlightable, TabletInteractionState.State highlightableState);
    public delegate void TabletToolUpdateAction(TabletHighlightable highlightable, TabletControlState controlState, TabletInteractionState.State highlightableState);
    public delegate void TabletToolInteractDelegate(TabletHighlightable highlightable, TabletControlState controlState, double timestamp);

    public enum TabletToolInteractionMode {
        None,
        Press,
        Hold
    }

    static public class TabletToolDefinitions {
        static public readonly TabletToolDefinition None = new TabletToolDefinition() { };

        static public readonly TabletToolDefinition Scan = new TabletToolDefinition() {
            RaycastMask = TabletUtility.DefaultSearchMask,

            GetState = (h, c, t) => {
                if (!h.CachedInteraction || !TabletInteractionUtility.HasInteractions(h, h.CachedInteraction)) {
                    return TabletInteractionState.State.Unavailable;
                }

                if (!TabletInteractionUtility.CanInteract(h.CachedInteraction, t)) {
                    return TabletInteractionState.State.Disabled;
                }

                return TabletInteractionState.State.Available;
            },

            DefaultVerb = "Interact",
            GetVerb = (h, hs) => {
                switch (h.CachedInteraction.Verb) {
                    case TabletInteractableVerb.Identify:
                        return "Identify";
                    case TabletInteractableVerb.Interact:
                        return "Interact";
                    case TabletInteractableVerb.Scan:
                        return "Scan";
                    default:
                        return null;
                }
            },

            InteractMode = TabletToolInteractionMode.Press,
            Interact = (h, c, t) => {
                if (TabletInteractionUtility.TryInteract(h, h.CachedInteraction, t)) {
                    TabletUtility.PlayHaptics(0.3f, 0.05f);
                }
            }
        };

        static public readonly TabletToolDefinition Capture = new TabletToolDefinition() {
            // TODO: Implement
        };

        static public readonly TabletToolDefinition Count = new TabletToolDefinition() {
            RaycastMask = TabletUtility.DefaultSearchMask,

            HighlightPredicate = (h, hc) => {
                return h.CachedCountable && TabletUtility.IsButtonHeld(XRHandButtons.TriggerButton);
            },

            GetState = (h, c, t) => {
                if (!h.CachedCountable || !TabletCountUtility.IsCountable(h.CachedCountable)) {
                    return TabletInteractionState.State.Unavailable;
                }

                return TabletInteractionState.State.Available;
            },

            DefaultVerb = "Count",

            InteractMode = TabletToolInteractionMode.Hold,
            Interact = (h, c, t) => {
                // TODO: count
                if(TabletCountUtility.TryCount(h, h.CachedCountable, t)) {
                    TabletUtility.PlayHaptics(0.3f, 0.05f);
                }
            }
        };

        static public readonly TabletToolDefinition Warp = new TabletToolDefinition() {
            RaycastMask = TabletUtility.TravelSearchMask,

            HighlightPredicate = (h, hc) => {
                return h.CachedWarp && h.CachedWarp.CanWarp;
            },

            GetState = (h, c, t) => {
                if (!h.CachedWarp || !h.CachedWarp.CanWarp) {
                    return TabletInteractionState.State.Unavailable;
                }

                return TabletInteractionState.State.Available;
            },

            DefaultVerb = "Move",

            InteractMode = TabletToolInteractionMode.Press,
            Interact = (h, c, t) => {
                PlayerMovementState moveState = Find.State<PlayerMovementState>();
                if (PlayerMovementUtility.WarpTo(moveState, h.CachedWarp)) {
                    TabletUtility.PlayHaptics(0.3f, 0.05f);
                }
            }
        };

        static private TabletToolDefinition[] s_ToolMap = new TabletToolDefinition[] {
            None, Scan, None, Count, Warp
        };

        static public TabletToolDefinition Get(TabletTool tool) {
            int idx = (int) tool;
            if (idx < 0 || idx >= s_ToolMap.Length) {
                throw new ArgumentOutOfRangeException("tool");
            }
            return s_ToolMap[idx];
        }
    }
}