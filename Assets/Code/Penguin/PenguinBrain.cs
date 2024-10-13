using System;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Audio;
using FieldDay.Components;
using FieldDay.Processes;
using FieldDay.Scripting;
using Leaf.Runtime;
using UnityEngine;

namespace Pennycook {
    public sealed class PenguinBrain : ProcessBehaviour, IScriptActorComponent {
        [Header("Components")]
        public Transform Position;
        public PenguinAnimator Animator;
        public PenguinRelationshipTracker Relationships;
        public PenguinNavigator Navigator;
        public AudioSource Voice;

        [Header("Configuration")]
        public PenguinPersonality Personality;
        public PenguinType Type;
        public NavPost Nest;

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
            base.Signal(signalId, signalArgs);
            m_ActionProcess.Signal(signalId, signalArgs);
            m_LookProcess.Signal(signalId, signalArgs);
        }

        #endregion // Signal

        #region IScriptActorComponent

        public ScriptActor Actor { get { return m_Actor; } }

        void IScriptActorComponent.OnScriptDeregister(ScriptActor actor) {
        }

        void IScriptActorComponent.OnScriptRegister(ScriptActor actor) {
        }

        void IScriptActorComponent.OnScriptSceneReady(ScriptActor actor) {

            switch (Type) {
                case PenguinType.Adult:
                case PenguinType.Banded:
                case PenguinType.Subadult:
                    StartMainProcess(PenguinSchedules.Wander);
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

        #endregion // Leaf
    }

    public enum PenguinType {
        Adult,
        Subadult,
        Chick,
        Banded
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