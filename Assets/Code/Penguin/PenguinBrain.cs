using System;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Audio;
using FieldDay.Components;
using FieldDay.Filters;
using FieldDay.Processes;
using FieldDay.Scripting;
using Leaf.Runtime;
using UnityEngine;

namespace Pennycook {
    public sealed class PenguinBrain : ProcessBehaviour, IScriptActorComponent, IComponentData {
        [Header("Components")]
        public Transform Position;
        [Required] public PenguinAnimator Animator;
        [Required] public PenguinRelationshipTracker Relationships;
        [Required] public PenguinNavigator Navigator;
        [Required] public PenguinContacts Contacts;
        public AudioSource Voice;

        [Header("Configuration")]
        public PenguinPersonality Personality;
        public PenguinType Type;
        public NavPost Nest;

        [NonSerialized] public PenguinMentalState MentalState;

        private ProcessId m_ActionProcess;
        private ProcessId m_LookProcess;

        [NonSerialized] private ScriptActor m_Actor;

        #region Action State

        public void ChangeActionState(ProcessStateDefinition state) {
            m_ActionProcess.TransitionTo(state);
        }

        public void ChangeActionState<TArg>(ProcessStateDefinition state, in TArg arg) where TArg : unmanaged {
            m_ActionProcess.TransitionTo(state, arg);
        }

        #endregion // Action State

        #region Signal

        public override void Signal(StringHash32 signalId, object signalArgs = null) {
            m_ActionProcess.Signal(signalId, signalArgs);
            m_LookProcess.Signal(signalId, signalArgs);
            base.Signal(signalId, signalArgs);
        }

        #endregion // Signal

        #region IScriptActorComponent

        public ScriptActor Actor { get { return m_Actor; } }

        void IScriptActorComponent.OnScriptDeregister(ScriptActor actor) {
            Game.Components.Deregister(this);
        }

        void IScriptActorComponent.OnScriptRegister(ScriptActor actor) {
            Game.Components.Register(this);
        }

        void IScriptActorComponent.OnScriptSceneReady(ScriptActor actor) {

            switch (Type) {
                case PenguinType.Adult:
                case PenguinType.Banded:
                case PenguinType.Subadult:
                    if(Nest == null) {
                        StartMainProcess(PenguinSchedules.Wander);
                    }
                    break;

                case PenguinType.Chick:
                    break;

            }

            m_ActionProcess = StartProcess(PenguinStates.Idle);
        }

        #endregion // IScriptActorComponent

        #region Leaf

        [LeafMember("SetPersonalityProfile")]
        private void LeafSetPersonalityProfile(StringHash32 profileId) {
            Personality = Find.NamedAsset<PenguinPersonality>(profileId);
            Log.Msg("[PenguinBrain] Updated '{0}' personality to '{1}'", m_Actor.Id, profileId);
        }

        [LeafMember("SetNest")]
        private void LeafSetNest(StringHash32 postId) {
            if (!PenguinNav.TryFindNamedNavPost(postId, out NavPost post)) {
                Log.Error("[PenguinBrain] No post '{0}' found - unable to set new nest for '{1}'", postId, m_Actor.Id);
                return;
            }

            Nest = post;
            Log.Msg("[PenguinBrain] Update '{0}' nest to '{1}'", m_Actor.Id, postId);
        }

        [LeafMember("SetMatingDance")]
        private void LeafSetMatingDace() {
            StartMainProcess(PenguinSchedules.MatingDance);
        }

        [LeafMember("SetRegurgitation")]
        private void LeafSetRegurgitation() {
            StartMainProcess(PenguinSchedules.Regurgitation);
        }


        [LeafMember("SetWeighGate")]
        private void LeafSetWeighGate() {
            StartMainProcess(PenguinSchedules.WeighGate);
        }

        #endregion // Leaf
    }

    public enum PenguinType {
        Adult,
        Subadult,
        Chick,
        Banded
    }

    public struct PenguinMentalState {
        public AnalogSignal FamilyAnxiety;
        public AnalogSignal PlayerAnxiety;
        public AnalogSignal SocialAnxiety;
        public float WanderRestlessness;
    }

    static public partial class PenguinUtility {
        /// <summary>
        /// Plays a vocalization from the penguin's mouth.
        /// </summary>
        static public void Vocalize(PenguinBrain brain, StringHash32 sfxId) {
            Sfx.PlayFrom(sfxId, brain.Voice);
        }
    }
}