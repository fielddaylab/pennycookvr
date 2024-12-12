#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

using System;
using System.Collections.Generic;
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
            public float PreloadWorkerTimeSlice;
        }

        #endregion // Types

        public const int MaxVoices = 80;
        public const int MaxBuses = 16;

        #region State

        private GameObject m_AudioSourceRoot;
        private AudioEmitterConfig m_DefaultEmitterConfig;
        private bool m_HasSpatializationPlugin;
        private float m_PreloadWorkerTimeSlice;

        private Pipe<AudioCommand> m_CommandPipe = new Pipe<AudioCommand>(128, true);
        private UniqueIdAllocator16 m_VoiceIdAllocator = new UniqueIdAllocator16(MaxVoices + MaxBuses);

        private Unsafe.ArenaHandle m_Arena;
        private UnsafeResourcePool<AudioPropertyBlock> m_TargetablePropertyBlocks;

        private LLTable<PositionSyncData> m_PositionSyncTable;
        private LLIndexList m_PositionSyncList;

        private LLTable<FloatParamTweenData> m_FloatTweenTable;
        private LLIndexList m_FloatTweenList;

        private IPool<AudioVoiceComponents> m_VoiceComponentPool;
        private IPool<VoiceData> m_VoiceDataPool;

        private BusData[] m_BusData;
        private int m_BusCount;

#if DEVELOPMENT
        private AudioPropertyBlock[] m_DebugBusProperties;
#endif // DEVELOPMENT

        private RingBuffer<VoiceData> m_ActiveVoices = new RingBuffer<VoiceData>(MaxVoices);
        private RingBuffer<AudioClip> m_PreloadQueue = new RingBuffer<AudioClip>(32, RingBufferMode.Expand);

        private readonly Dictionary<uint, int> m_BusNameToIndex = new Dictionary<uint, int>(MaxBuses);

        #endregion // State

        internal unsafe AudioMgr(Config config) {
            m_Arena = Unsafe.CreateArena(1 * Unsafe.MiB, "Audio", Unsafe.AllocatorFlags.ZeroOnAllocate);
            m_TargetablePropertyBlocks.Create(m_Arena, (MaxVoices + MaxBuses) * 2);
            m_PreloadWorkerTimeSlice = config.PreloadWorkerTimeSlice;

            m_PositionSyncTable = new LLTable<PositionSyncData>(MaxVoices);
            m_FloatTweenTable = new LLTable<FloatParamTweenData>((MaxVoices + MaxBuses) * 2);
            m_PositionSyncList = m_FloatTweenList = LLIndexList.Empty;

            m_VoiceDataPool = new FixedPool<VoiceData>(MaxVoices, Pool.DefaultConstructor<VoiceData>());
            m_VoiceDataPool.Prewarm(MaxVoices);

            m_BusData = new BusData[MaxBuses];
            for(int i = 0; i < MaxBuses; i++) {
                ref BusData bus = ref m_BusData[i];
                bus.ScriptProperties = m_TargetablePropertyBlocks.Alloc();

                *bus.ScriptProperties = AudioPropertyBlock.Default;
                bus.LastKnownProperties = AudioPropertyBlock.Default;

                bus.Handle = m_VoiceIdAllocator.Alloc();
                bus.FloatTweens.Reset();
            }

#if DEVELOPMENT
            for(int i = 0; i < MaxBuses; i++) {
                m_DebugBusProperties[i] = AudioPropertyBlock.Default;
            }
#endif // DEVELOPMENT

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
                a.PlayingHandle = default;
                a.gameObject.SetActive(false);
            });
            m_VoiceComponentPool.Prewarm(MaxVoices);

            if (config.DefaultEmitterProfile) {
                m_DefaultEmitterConfig = config.DefaultEmitterProfile.Config;
                Game.Assets.AddNamed(config.DefaultEmitterProfile.name, config.DefaultEmitterProfile);
            } else {
                m_DefaultEmitterConfig = config.Is3D ? AudioEmitterConfig.Default3D : AudioEmitterConfig.Default2D;
            }

            m_HasSpatializationPlugin = !string.IsNullOrEmpty(AudioSettings.GetSpatializerPluginName());

            Game.Assets.SetNamedAssetLoadCallbacks<AudioEvent>(OnAudioEventLoaded, OnAudioEventUnloaded);

            m_BusNameToIndex.Add(0, 0);
            CreateBus(AudioBus.Master);
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

                if (m_PreloadQueue.Count > 0) {
                    WorkSlicer.TimeSliced(m_PreloadQueue, HandlePreloadDelegate, m_PreloadWorkerTimeSlice / 2);
                }
            }
        }

        internal void LateUpdate(float deltaTime) {
            using (Profiling.Sample("AudioMgr::LateUpdate")) {
                FlushCommandPipe();

                if (m_PreloadQueue.Count > 0) {
                    WorkSlicer.TimeSliced(m_PreloadQueue, HandlePreloadDelegate, m_PreloadWorkerTimeSlice / 2);
                }

                SyncEmitterLocations();
                UpdateTweens(deltaTime);
                UpdateVoices(deltaTime, Time.realtimeSinceStartupAsDouble);

                switch (Frame.Index % 60) {
                    case 0: {
                        m_FloatTweenTable.Linearize(ref m_FloatTweenList);
                        break;
                    }
                    case 1: {
                        m_FloatTweenTable.OptimizeFreeList();
                        break;
                    }
                    case 2: {
                        m_PositionSyncTable.Linearize(ref m_PositionSyncList);
                        break;
                    }
                    case 3: {
                        m_PositionSyncTable.OptimizeFreeList();
                        break;
                    }
                }
            }
        }

        internal void Shutdown() {
            Unsafe.TryDestroyArena(ref m_Arena);
            m_TargetablePropertyBlocks = default;

            m_VoiceComponentPool.Clear();
        }

        #endregion // Events

        #region Asset Handlers

        static private WorkSlicer.ElementOperation<AudioClip> HandlePreloadDelegate = HandlePreload;

        static private void HandlePreload(AudioClip clip) {
            if (clip.loadState == AudioDataLoadState.Unloaded) {
                using (Profiling.Time("AudioMgr.HandlePreload", ProfileTimeUnits.Microseconds)) {
                    clip.LoadAudioData();
                }
                Log.Debug("[AudioMgr] Preloaded clip '{0}'", clip.name);
            }
        }

        private void OnAudioEventLoaded(AudioEvent evt) {
            foreach(var clip in evt.Samples) {
                if (!clip.preloadAudioData && clip.loadState == AudioDataLoadState.Unloaded) {
                    m_PreloadQueue.PushBack(clip);
                }
            }
        }

        private void OnAudioEventUnloaded(AudioEvent evt) {

        }

        #endregion // Asset Handlers

        #region Command Pipe

        internal void QueueAudioCommand(in AudioCommand cmd) {
            m_CommandPipe.Write(cmd);
        }

        internal AudioHandle QueuePlayAudioCommand(AudioCommand cmd) {
            UniqueId16 id = m_VoiceIdAllocator.Alloc();
            cmd.Play.Handle = id;
            m_CommandPipe.Write(cmd);
            return new AudioHandle(id);
        }

        #endregion // Command Pipe

        #region Preload

        /// <summary>
        /// Queues the given clip to be preloaded.
        /// </summary>
        public void QueuePreload(AudioClip clip) {
            if (clip != null && clip.loadState == AudioDataLoadState.Unloaded) {
                m_PreloadQueue.PushBack(clip);
            }
        }

        /// <summary>
        /// Queues the given clip to be preloaded immediately.
        /// </summary>
        public void PushPreloadImmediate(AudioClip clip) {
            if (clip != null && clip.loadState == AudioDataLoadState.Unloaded) {
                m_PreloadQueue.PushFront(clip);
            }
        }

        #endregion // Preload
    }
}