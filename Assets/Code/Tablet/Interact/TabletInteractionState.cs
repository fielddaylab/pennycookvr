using System;
using FieldDay;
using FieldDay.SharedState;
using FieldDay.UI.Animation;
using TMPro;
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
        public FadeGroup InteractionGroup;
        public TMP_Text InteractionLabel;
        [Space]
        public FadeGroup DetailsGroup;
        public GameObject DetailsHeaderGroup;
        public TMP_Text DetailsHeader;
        public GameObject DetailsDescriptionGroup;
        public TMP_Text DetailsDescription;
        [Space]
        public FadeGroup IdentifiedGroup;
        public TMP_Text IdentifiedLabel;
        
        [NonSerialized] public State CurrentState = State.Disabled;

        void IRegistrationCallbacks.OnDeregister() {
        }

        void IRegistrationCallbacks.OnRegister() {
        }
    }
}