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

            TabletInteractionState.State desiredState = UpdateState(ts);

            var updateFunc = m_StateC.CurrentToolDef.Update;
            if (updateFunc != null && desiredState != TabletInteractionState.State.Disabled) {
                updateFunc(m_StateB.HighlightedObject, m_StateD, desiredState);
                desiredState = UpdateState(ts);
            }

            if (desiredState == TabletInteractionState.State.Available) {
                bool isPressing = false;
                switch (m_StateC.CurrentToolDef.InteractMode) {
                    case TabletToolInteractionMode.Press:
                        isPressing = TabletUtility.ConsumeButtonPress(XRHandButtons.TriggerButton);
                        break;
                    case TabletToolInteractionMode.Hold:
                        isPressing = TabletUtility.IsButtonHeld(XRHandButtons.TriggerButton);
                        break;
                }

                if (isPressing) {
                    DoInteraction(ts);
                }
            }
        }

        private void DoInteraction(double timestamp) {
            var func = m_StateC.CurrentToolDef.Interact;
            if (func != null) {
                func(m_StateB.HighlightedObject, m_StateD, timestamp);
            }
        }

        private TabletInteractionState.State UpdateState(double ts) {
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
                        string verb = m_StateC.CurrentToolDef.GetVerb?.Invoke(m_StateB.HighlightedObject, desiredState) ?? m_StateC.CurrentToolDef.DefaultVerb;
                        m_StateA.InteractionGroup.gameObject.SetActive(true);
                        m_StateA.InteractionGroup.alpha = 0.5f;

                        m_StateA.InteractionLabel.SetText(verb);
                        break;
                    }

                    case TabletInteractionState.State.Available: {
                        string verb = m_StateC.CurrentToolDef.GetVerb?.Invoke(m_StateB.HighlightedObject, desiredState) ?? m_StateC.CurrentToolDef.DefaultVerb;
                        m_StateA.InteractionGroup.gameObject.SetActive(true);
                        m_StateA.InteractionGroup.alpha = 1;

                        m_StateA.InteractionLabel.SetText(verb);
                        break;
                    }
                }
            }
            return desiredState;
        }

        private TabletInteractionState.State GetDesiredState(double timestamp) {
            var func = m_StateC.CurrentToolDef.GetState;
            if (m_StateD.GrippedHandMask.IsEmpty || !m_StateB.HighlightedObject || func == null) {
                return TabletInteractionState.State.Disabled;
            }

            PlayerMovementState moveState = Find.State<PlayerMovementState>();
            if (moveState.CurrentState == PlayerMovementState.State.Warping || m_StateC.CurrentTool == TabletTool.None) {
                return TabletInteractionState.State.Disabled;
            }

            return func(m_StateB.HighlightedObject, m_StateD, timestamp); 
        }
    }
}