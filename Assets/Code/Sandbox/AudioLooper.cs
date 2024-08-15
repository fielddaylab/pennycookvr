using System;
using BeauUtil;
using FieldDay.Components;
using UnityEngine;
using FieldDay.Audio;
using FieldDay;

namespace Pennycook {
    public class AudioLooper : BatchedComponent, IRegistrationCallbacks {
        [AudioEventRef] public StringHash32 EventId;
        public Transform Location;

        [NonSerialized] private AudioHandle m_PlaybackHandle;

        void IRegistrationCallbacks.OnDeregister() {
            Sfx.Stop(m_PlaybackHandle);
            m_PlaybackHandle = default;
        }

        void IRegistrationCallbacks.OnRegister() {
            if (!Location) {
                Location = transform;
            }
            Game.Scenes.QueueOnLoad(this, OnLoaded);
        }

        private void OnLoaded() {
            Sfx.Stop(m_PlaybackHandle);
            m_PlaybackHandle = Sfx.Play(EventId, Location);
        }
    }
}