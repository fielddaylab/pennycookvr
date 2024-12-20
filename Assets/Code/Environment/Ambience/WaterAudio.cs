using System;
using BeauUtil;
using FieldDay;
using FieldDay.Audio;
using FieldDay.Debugging;
using FieldDay.SharedState;
using UnityEngine;

namespace Pennycook {
    public sealed class WaterAudio : MonoBehaviour {
        public float EdgeDistance;
        public AudioSource Emitter;
        [AudioEventRef] public StringHash32 WaterSfx;

        [NonSerialized] public AudioHandle SfxHandle;
        [NonSerialized] private Vector3 m_CachedPosition;

        private void Awake() {
            Game.Scenes.QueueOnEnable(() => {
                SfxHandle = Sfx.PlayFrom(WaterSfx, Emitter);
            });

            m_CachedPosition = transform.position;
        }

        private void OnDestroy() {
            Sfx.Stop(SfxHandle);
        }

        private void LateUpdate() {
            if (GameLoop.IsLoading) {
                return;
            }

            Vector3 myPosition =
#if UNITY_EDITOR
                transform.position;
#else
                m_CachedPosition;
#endif // UNITY_EDITOR
            var referenceListener = Game.Audio.Listener.transform.position;
            referenceListener.y = myPosition.y;

            Vector3 vec = (referenceListener - myPosition).normalized;
            Emitter.transform.position = myPosition + vec * EdgeDistance;

            DebugDraw.AddSphere(Emitter.transform.position, 5, Color.blue);
        }

        private void OnDrawGizmos() {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, EdgeDistance);
        }
    }
}