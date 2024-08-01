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
                    TabletInteractionUtility.TryInteract(m_StateB.HighlightedObject.CachedInteraction, ts);
                }
            }
        }

        private TabletInteractionState.State GetDesiredState(double timestamp) {
            if (m_StateC.CurrentTool != TabletTool.Scan) {
                return TabletInteractionState.State.Disabled;
            }

            if (m_StateD.GrippedHandMask.IsEmpty || !m_StateB.HighlightedObject || !m_StateB.HighlightedObject.CachedInteraction || !TabletInteractionUtility.HasInteractions(m_StateB.HighlightedObject.CachedInteraction)) {
                return TabletInteractionState.State.Unavailable;
            }

            if (!TabletInteractionUtility.CanInteract(m_StateB.HighlightedObject.CachedInteraction, timestamp)) {
                return TabletInteractionState.State.Disabled;
            }

            return TabletInteractionState.State.Available;
        }
    }
}