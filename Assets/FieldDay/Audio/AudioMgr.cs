using System;
using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Pipes;
using UnityEngine;

namespace FieldDay.Audio {
    public sealed partial class AudioMgr {
        #region Types

        [Serializable]
        public struct Config {
            public bool Is3D;
            public AudioEmitterProfile DefaultEmitterProfile;
        }

        #endregion // Types

        public const int MaxVoices = 80;
        public const int MaxBuses = 16;

        #region State

        private GameObject m_AudioSourceRoot;
        private AudioEmitterConfig m_DefaultEmitterConfig;

        private Pipe<AudioCommand> m_CommandPipe = new Pipe<AudioCommand>(128, true);
        private UniqueIdAllocator16 m_VoiceIdAllocator = new UniqueIdAllocator16(MaxVoices);

        private Unsafe.ArenaHandle m_Arena;
        private UnsafeResourcePool<AudioPropertyBlock> m_TargetablePropertyBlocks;

        private LLTable<PositionSyncData> m_PositionSyncTable;
        private LLIndexList m_PositionSyncList;

        private LLTable<FloatParamTweenData> m_FloatTweenTable;
        private LLIndexList m_FloatTweenList;

        private IPool<AudioVoiceComponents> m_VoiceComponentPool;
        private IPool<VoiceData> m_VoiceDataPool;

        private RingBuffer<VoiceData> m_ActiveVoices = new RingBuffer<VoiceData>(MaxVoices);

        #endregion // State

        internal AudioMgr(Config config) {
            m_Arena = Unsafe.CreateArena(1 * Unsafe.MiB, "Audio", Unsafe.AllocatorFlags.ZeroOnAllocate);
            m_TargetablePropertyBlocks.Create(m_Arena, (MaxVoices + MaxBuses) * 2);

            m_PositionSyncTable = new LLTable<PositionSyncData>(MaxVoices);
            m_FloatTweenTable = new LLTable<FloatParamTweenData>(MaxVoices * 2);
            m_PositionSyncList = m_FloatTweenList = LLIndexList.Empty;

            m_VoiceDataPool = new FixedPool<VoiceData>(MaxVoices, Pool.DefaultConstructor<VoiceData>());
            m_VoiceDataPool.Prewarm(MaxVoices);

            m_AudioSourceRoot = new GameObject("AudioMgr");
            m_AudioSourceRoot.hideFlags |= HideFlags.NotEditable | HideFlags.DontSave;
            GameObject.DontDestroyOnLoad(m_AudioSourceRoot);

            m_VoiceComponentPool = new FixedPool<AudioVoiceComponents>(MaxVoices, ConstructNewSource);
            m_VoiceComponentPool.Config.RegisterOnDestruct((p, a) => GameObject.Destroy(a.gameObject));
            m_VoiceComponentPool.Config.RegisterOnFree((p, a) => {
                a.Source.Stop();
                a.Source.clip = null;
#if UNITY_EDITOR
                a.gameObject.name = "unused audio voice";
#endif // UNITY_EDITOR
                a.enabled = false;
                a.gameObject.SetActive(false);
            });
            m_VoiceComponentPool.Prewarm(MaxVoices);

            if (config.DefaultEmitterProfile) {
                m_DefaultEmitterConfig = config.DefaultEmitterProfile.Config;
                Game.Assets.AddNamed(config.DefaultEmitterProfile.name, config.DefaultEmitterProfile);
            } else {
                m_DefaultEmitterConfig = config.Is3D ? AudioEmitterConfig.Default3D : AudioEmitterConfig.Default2D;
            }
        }

        #region Events

        internal void PreUpdate(float deltaTime) {
            using (Profiling.Sample("AudioMgr::PreUpdate")) {
                CullFinishedVoices();
                FlushCommandPipe();
            }
        }

        internal void Update(float deltaTime) {
            using (Profiling.Sample("AudioMgr::Update")) {
                FlushCommandPipe();
            }
        }

        internal void LateUpdate(float deltaTime) {
            using (Profiling.Sample("AudioMgr::LateUpdate")) {
                FlushCommandPipe();

                SyncEmitterLocations();
                UpdateTweens(deltaTime);
                UpdateVoices(deltaTime, Time.realtimeSinceStartupAsDouble);

                if (Frame.Interval(60, 0)) {
                    m_FloatTweenTable.Linearize(ref m_FloatTweenList);
                    m_FloatTweenTable.OptimizeFreeList();
                } else if (Frame.Interval(60, 1)) {
                    m_PositionSyncTable.Linearize(ref m_PositionSyncList);
                    m_PositionSyncTable.OptimizeFreeList();
                }
            }
        }

        internal void Shutdown() {
            Unsafe.TryDestroyArena(ref m_Arena);
            m_TargetablePropertyBlocks = default;

            m_VoiceComponentPool.Clear();
        }

        #endregion // Events

        internal void QueueAudioCommand(in AudioCommand cmd) {
            m_CommandPipe.Write(cmd);
        }

        internal UniqueId16 QueuePlayAudioCommand(AudioCommand cmd) {
            UniqueId16 id = m_VoiceIdAllocator.Alloc();
            cmd.Play.Handle = id;
            m_CommandPipe.Write(cmd);
            return id;
        }
    }
}