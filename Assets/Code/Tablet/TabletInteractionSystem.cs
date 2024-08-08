using FieldDay;
using FieldDay.HID.XR;
using FieldDay.Systems;
using FieldDay.XR;
using UnityEngine;

namespace Pennycook.Tablet {
    [SysUpdate(GameLoopPhase.Update, 11)]
    public class TabletInteractionSystem : SharedStateSystemBehaviour<TabletInteractionState, TabletHighlightState, TabletToolState, TabletControlState> {
        public override void ProcessWork(float deltaTime) {
            double ts = Time.timeAsDouble;

            TabletInteractionState.State desiredState = GetDesiredState(ts);
            if (m_StateA.CurrentState != desiredState) {
                m_StateA.CurrentState = desiredState;

                switch (desiredState) {
                    case TabletInteractionState.State.Disabled:
                    case TabletInteractionState.State.Unavailable: {
                        m_StateA.InteractionGroup.gameObject.SetActive(false);
                        break;
                    }

                    case TabletInteractionState.State.Waiting: {
                        m_StateA.InteractionGroup.gameObject.SetActive(true);
                        m_StateA.InteractionGroup.alpha = 0.5f;
                        break;
                    }

                    case TabletInteractionState.State.Available: {
                        m_StateA.InteractionGroup.gameObject.SetActive(true);
                        m_StateA.InteractionGroup.alpha = 1;
                        break;
                    }
                }
            }

            if (desiredState == TabletInteractionState.State.Available && m_StateD.GrippedHandMask.IsSet((int) XRHandIndex.Right)) {
                XRInputState input = Find.State<XRInputState>();
                if (input.RightHand.Buttons.ConsumePress(XRHandButtons.Primary)) {
                    DoInteraction(ts);
                }
            }
        }

        private void DoInteraction(double timestamp) {
            switch (m_StateC.CurrentTool) {
                case TabletTool.Scan: {
                    TabletInteractionUtility.TryInteract(m_StateB.HighlightedObject.CachedInteraction, timestamp);
                    break;
                }

                case TabletTool.Move: {
                    PlayerMovementState moveState = Find.State<PlayerMovementState>();
                    PlayerMovementUtility.WarpTo(moveState, m_StateB.HighlightedObject.CachedWarp);
                    break;
                }
            }
        }

        private TabletInteractionState.State GetDesiredState(double timestamp) {
            PlayerMovementState moveState = Find.State<PlayerMovementState>();
            if (moveState.CurrentState == PlayerMovementState.State.Warping) {
                return TabletInteractionState.State.Disabled;
            }

            switch (m_StateC.CurrentTool) {
                case TabletTool.Scan: {
                    if (m_StateD.GrippedHandMask.IsEmpty || !m_StateB.HighlightedObject || !m_StateB.HighlightedObject.CachedInteraction || !TabletInteractionUtility.HasInteractions(m_StateB.HighlightedObject.CachedInteraction)) {
                        return TabletInteractionState.State.Unavailable;
                    }

                    if (!TabletInteractionUtility.CanInteract(m_StateB.HighlightedObject.CachedInteraction, timestamp)) {
                        return TabletInteractionState.State.Disabled;
                    }

                    return TabletInteractionState.State.Available;
                }

                case TabletTool.Move: {
                    if (m_StateD.GrippedHandMask.IsEmpty || !m_StateB.HighlightedObject || !m_StateB.HighlightedObject.CachedWarp || !m_StateB.HighlightedObject.CachedWarp.CanWarp) {
                        return TabletInteractionState.State.Unavailable;
                    }

                    return TabletInteractionState.State.Available;
                }

                default:
                    return TabletInteractionState.State.Unavailable;
            }
        }
    }
}