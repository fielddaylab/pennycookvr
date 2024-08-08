using System;
using BeauUtil;
using FieldDay.Pipes;

namespace FieldDay.Audio {
    public sealed class AudioMgr {
        #region Types

        [Serializable]
        public struct Config {
        }

        #endregion // Types

        public const int MaxSampleVoices = 128;
        public const int MaxStreamVoices = 16;
        public const int MaxBuses = 16;

        #region State

        private Pipe<AudioCommand> m_CommandPipe = new Pipe<AudioCommand>(64, true);
        private UniqueIdAllocator16 m_IdAllocator = new UniqueIdAllocator16(64);

        private Unsafe.ArenaHandle m_Arena;

        #endregion // State

        internal AudioMgr(Config config) {
            //m_Arena = Unsafe.CreateArena(2 * Unsafe.MiB, "Audio", Unsafe.AllocatorFlags.ZeroOnAllocate);
        }

        #region Events

        internal void PreUpdate(float deltaTime) {

        }

        internal void Update(float deltaTime) {

        }

        internal void Shutdown() {
            //Unsafe.TryDestroyArena(ref m_Arena);
        }

        #endregion // Events

        #region Audio Updates

        private void UpdateEmitterLocations(float deltaTime) {

        }

        private void UpdateMix(float deltaTime) {

        }

        private void UpdatePlayback(float deltaTime) {

        }

        #endregion // Audio Updates
    }
}