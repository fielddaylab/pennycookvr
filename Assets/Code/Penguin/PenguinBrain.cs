using System;
using BeauUtil;
using FieldDay.Audio;
using FieldDay.Components;
using FieldDay.Processes;
using FieldDay.Scripting;
using UnityEngine;

namespace Pennycook {
    public sealed class PenguinBrain : ProcessBehaviour, IScriptActorComponent {
        public Transform Position;
        public PenguinAnimator Animator;
        public AudioSource Voice;

        private ProcessId m_ActionProcess;
        private ProcessId m_LookProcess;

        [NonSerialized] private ScriptActor m_Actor;

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
            
        }

        #endregion // IScriptActorComponent
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