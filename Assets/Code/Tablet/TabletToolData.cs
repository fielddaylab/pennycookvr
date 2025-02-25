using System;
using BeauUtil;
using FieldDay;
using FieldDay.HID.XR;
using FieldDay.UI;
using FieldDay.XR;
using UnityEngine;

namespace Pennycook.Tablet {
    public class TabletToolDefinition {
        public LayerMask RaycastMask;
        public float RaycastBaseDistance = 25;
        public float RaycastUnitConeRadius;
        public bool ShowReticle = true;

        public TabletToolFlags Flags;

        public Predicate<TabletHighlightable, TabletHighlightState> HighlightPredicate;
        public TabletToolHighlightEventDelegate OnHighlighted;
        public TabletToolHighlightEventDelegate OnUnhighlighted;

        public TabletToolAnalyzePredicate GetState;
        
        public string DefaultVerb;
        public TabletToolInteractionVerbPredicate GetVerb;

        public TabletToolUpdateAction Update;

        public TabletToolInteractionMode InteractMode;
        public TabletToolInteractDelegate Interact;

        public TabletToolEventDelegate OnOpen;
        public TabletToolEventDelegate OnClose;
    }

    public delegate TabletInteractionState.State TabletToolAnalyzePredicate(TabletHighlightable highlightable, TabletControlState controlState, double timestamp);
    public delegate string TabletToolInteractionVerbPredicate(TabletHighlightable highlightable, TabletInteractionState.State highlightableState);
    public delegate void TabletToolUpdateAction(TabletHighlightable highlightable, TabletControlState controlState, TabletInteractionState.State highlightableState);
    public delegate void TabletToolInteractDelegate(TabletHighlightable highlightable, TabletControlState controlState, double timestamp);
    public delegate void TabletToolHighlightEventDelegate(TabletHighlightable highlightable, TabletControlState controlState);
    public delegate void TabletToolEventDelegate(TabletControlState controlState);

    public enum TabletToolInteractionMode {
        None,
        Press,
        Hold,
        Always
    }

    [Flags]
    public enum TabletToolFlags {
        DoNotUseHighlightBox = 0x01,
        NoPrompt = 0x02,
        InteractionDoesNotRequireHighlight = 0x04,
        SkipHapticHighlightFeedback = 0x08
    }

    static public class TabletToolDefinitions {
        static public readonly TabletToolDefinition None = new TabletToolDefinition() { };

        static public readonly TabletToolDefinition Scan = new TabletToolDefinition() {
            RaycastMask = TabletUtility.DefaultSearchMask,
            RaycastUnitConeRadius = 0.07f,

            HighlightPredicate = (h, hc) => {
                return h.CachedInteraction && TabletInteractionUtility.HasInteractions(h, h.CachedInteraction);
            },

            GetState = (h, c, t) => {
                if (!TabletInteractionUtility.CanInteract(h.CachedInteraction, t)) {
                    return TabletInteractionState.State.Disabled;
                }

                return TabletInteractionState.State.Available;
            },

            DefaultVerb = "Scan",
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
                    if (h.Identified && !string.IsNullOrEmpty(h.Contents.SimpleHeader)) {
                        TabletInteractionState iState = Find.State<TabletInteractionState>();
                        iState.IdentifiedLabel.SetText(h.Contents.SimpleHeader);
                        iState.IdentifiedGroup.Show();
                    }
                }
            },

            OnHighlighted = (h, c) => {
                if (h.Identified && !string.IsNullOrEmpty(h.Contents.SimpleHeader)) {
                    TabletInteractionState iState = Find.State<TabletInteractionState>();
                    iState.IdentifiedLabel.SetText(h.Contents.SimpleHeader);
                    iState.IdentifiedGroup.Show();
                }
            },

            OnUnhighlighted = (h, c) => {
                TabletInteractionState iState = Find.State<TabletInteractionState>();
                iState.DetailsGroup.Hide();
                iState.IdentifiedGroup.Hide();
            }
        };

        static public readonly TabletToolDefinition Capture = new TabletToolDefinition() {
            RaycastMask = TabletUtility.CaptureSearchMask,
            RaycastUnitConeRadius = 0.4f,

            Flags = TabletToolFlags.DoNotUseHighlightBox | TabletToolFlags.InteractionDoesNotRequireHighlight
                | TabletToolFlags.SkipHapticHighlightFeedback,

            HighlightPredicate = (h, hc) => {
                return h;
            },

            GetState = (h, c, t) => {
                TabletPhotoState photoState = Find.State<TabletPhotoState>();
                if (t < photoState.NextAllowedPhotoTS || photoState.CurrentStage != TabletPhotoState.Stage.Idle) {
                    return TabletInteractionState.State.Unavailable;
                }
                if (photoState.PhotoPool.Count == 0) {
                    return TabletInteractionState.State.Waiting;
                }
                return TabletInteractionState.State.Available;
            },

            DefaultVerb = "Capture",

            InteractMode = TabletToolInteractionMode.Press,
            Interact = (h, c, t) => {
                PhotoUtility.TakePhoto(h, t);
            },

            OnOpen = (c) => {
                Find.State<TabletToolState>().CaptureGroup.Show();
            },

            OnClose = (c) => {
                Find.State<TabletToolState>().CaptureGroup.Hide();
            }
        };

        static public readonly TabletToolDefinition Count = new TabletToolDefinition() {
            RaycastMask = TabletUtility.CountSearchMask,
            RaycastUnitConeRadius = 0.65f,
            ShowReticle = false,

            Flags = TabletToolFlags.DoNotUseHighlightBox | TabletToolFlags.NoPrompt,

            HighlightPredicate = (h, hc) => {
                return h.CachedCountable && TabletCountUtility.IsCountable(h.CachedCountable);
            },

            GetState = (h, c, t) => {
                if (!h.CachedCountable || !TabletCountUtility.IsCountable(h.CachedCountable)) {
                    return TabletInteractionState.State.Unavailable;
                }

                return TabletInteractionState.State.Available;
            },

            InteractMode = TabletToolInteractionMode.Always,
            Interact = (h, c, t) => {
                if(TabletCountUtility.TryCount(h, h.CachedCountable, t)) {
                    // TODO: visual effect
                    TabletUtility.PlayHaptics(0.3f, 0.05f);
                }
            },

            OnOpen = (c) => {
                Find.State<TabletToolState>().CountGroup.Show();
            },

            OnClose = (c) => {
                Find.State<TabletToolState>().CountGroup.Hide();
            }
        };

        static public readonly TabletToolDefinition Warp = new TabletToolDefinition() {
            RaycastMask = TabletUtility.TravelSearchMask,
            RaycastUnitConeRadius = 0.07f,

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
                    //TabletUtility.PlaySfx("Tablet.Warp");
                }
            },

            OnHighlighted = (h, c) => {
                Find.State<TabletHighlightState>().WarpGroup.SetActive(true);
            },

            OnUnhighlighted = (h, c) => {
                Find.State<TabletHighlightState>().WarpGroup.SetActive(false);
            }
        };

        static private TabletToolDefinition[] s_ToolMap = new TabletToolDefinition[] {
            None, Scan, Capture, Count, Warp
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