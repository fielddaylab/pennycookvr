using System;
using FieldDay;
using FieldDay.SharedState;
using UnityEngine;

namespace Pennycook.Tablet {
    public class TabletInteractionState : SharedStateComponent, IRegistrationCallbacks {
        public enum State {
            Disabled,
            Unavailable,
            Waiting,
            Available
        }
        
        [Header("Components")]
        public CanvasGroup InteractionGroup;
        
        [NonSerialized] public State CurrentState = State.Disabled;

        void IRegistrationCallbacks.OnDeregister() {
        }

        void IRegistrationCallbacks.OnRegister() {
        }
    }
}