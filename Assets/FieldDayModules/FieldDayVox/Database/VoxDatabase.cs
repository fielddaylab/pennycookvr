using System;
using System.Collections.Generic;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Scenes;
using FieldDay.SharedState;
using UnityEngine;
using UnityEngine.Networking;

namespace FieldDay.Vox {
    public sealed class VoxDatabase : ISharedState, IRegistrationCallbacks, ISceneLoadDependency {
        #region Config

        internal string BasePath = "vox";
        internal string LanguagePath = "en/";
        internal string FileExtension = ".mp3";

        #endregion // Config

        #region State

        internal Dictionary<StringHash32, VoxFileEntry> FileEntryMap;
        internal Dictionary<StringHash32, VoxFileData> LoadedFileDataMap;

        internal RingBuffer<StringHash32> LineCodeLoadQueue;
        internal RingBuffer<StringHash32> LineCodeUnloadQueue;
        internal RingBuffer<VoxFileData> FileUnloadQueue;

        internal StringHash32 CurrentLoadFileId;
        internal UnityWebRequest CurrentLoad;

        internal Dictionary<StringHash32, VoxEmitter> EmitterMap;

        internal Dictionary<StringHash32, string> LineCodeToReadableFileName;
        internal AsyncHandle LineCodeRemapReader;

        #endregion // State

        void IRegistrationCallbacks.OnDeregister() {
            Game.Scenes?.DeregisterLoadDependency(this);
        }

        void IRegistrationCallbacks.OnRegister() {
            FileEntryMap = MapUtils.Create<StringHash32, VoxFileEntry>(512);
            LoadedFileDataMap = MapUtils.Create<StringHash32, VoxFileData>(512);

            LineCodeLoadQueue = new RingBuffer<StringHash32>(32, RingBufferMode.Expand);
            LineCodeUnloadQueue = new RingBuffer<StringHash32>(32, RingBufferMode.Expand);
            FileUnloadQueue = new RingBuffer<VoxFileData>(64, RingBufferMode.Expand);

            EmitterMap = MapUtils.Create<StringHash32, VoxEmitter>(8);

            LineCodeToReadableFileName = MapUtils.Create<StringHash32, string>(1024);

            Game.Scenes?.RegisterLoadDependency(this);
        }

        #region ISceneLoadDependency

        bool ISceneLoadDependency.IsLoaded(SceneLoadPhase loadPhase) {
            if (LineCodeRemapReader.IsRunning()) {
                return false;
            }

            switch (loadPhase) {
                case SceneLoadPhase.BeforeLateEnable: {
                    return FileUnloadQueue.Count == 0 && LineCodeUnloadQueue.Count == 0;
                }
                case SceneLoadPhase.BeforeReady:
                default: {
                    return LineCodeLoadQueue.Count == 0;
                }
            }
        }

        #endregion // ISceneLoadDependency
    }

    public struct VoxFileData {
        public AudioClip Clip;
    }

    internal struct VoxFileEntry {
        public StringHash32 PathHash;
        public string Path;
    }

    static public partial class VoxUtility {
        [SharedStateReference] static public VoxDatabase DB { get; private set; }

        [InvokePreBoot]
        static private void Initialize() {
            Game.SharedState.Register(new VoxDatabase());
            Game.SharedState.Register(new VoxRequestState());
            Game.Systems.Register(new VoxLoadingSystem());
            Game.Systems.Register(new VoxRequestSystem());
        }

        #region Configuration

        /// <summary>
        /// Configures the base path and file extension for streaming voiceover files.
        /// </summary>
        static public void ConfigureStreamingPaths(string basePath, string fileExtension) {
            Assert.True(!string.IsNullOrEmpty(basePath), "basePath must not be null");
            Assert.True(!string.IsNullOrEmpty(fileExtension), "fileFormat must not be null");

            if (!fileExtension.StartsWith('.')) {
                fileExtension = "." + fileExtension;
            }

            DB.BasePath = basePath;
            DB.FileExtension = fileExtension;

            UnloadAll();
        }

        /// <summary>
        /// Configures the subdirectory currently being used for voiceover loading.
        /// Note that this will cancel all currently playing voiceover and unload all previously loaded files.
        /// </summary>
        static public void ConfigureLanguageDirectory(string languageDirectory) {
            if (DB.LanguagePath != languageDirectory) {
                DB.LanguagePath = languageDirectory;
                UnloadAll();
            }
        }

        #endregion // Configuration

        #region Loading

        /// <summary>
        /// Queues a line of voiceover to be loaded.
        /// </summary>
        static public void QueueLoad(StringHash32 lineCode) {
            DB.LineCodeLoadQueue.PushBack(lineCode);
        }

        /// <summary>
        /// Pushes a line of voiceover to be loaded next.
        /// </summary>
        static public void PushImmediateLoad(StringHash32 lineCode) {
            DB.LineCodeLoadQueue.PushFront(lineCode);
        }

        #endregion // Loading

        #region Unload

        /// <summary>
        /// Queues a line of voiceover to be unloaded.
        /// </summary>
        static public void QueueUnload(StringHash32 lineCode) {
            DB.LineCodeUnloadQueue.PushBack(lineCode);
        }

        /// <summary>
        /// Unloads all currently loaded files.
        /// </summary>
        static public void UnloadAll() {
            var db = DB;
            
            // Stops all currently playing voiceover
            foreach(var emitter in Find.Components<VoxEmitter>()) {
                Stop(emitter);
            }

            // queues up each loaded file to be manually unloaded
            foreach(var data in db.LoadedFileDataMap.Values) {
                db.FileUnloadQueue.PushBack(data);
            }

            db.FileEntryMap.Clear();
            db.LoadedFileDataMap.Clear();

            db.LineCodeLoadQueue.Clear();
            db.LineCodeUnloadQueue.Clear();

            if (db.CurrentLoad != null) {
                db.CurrentLoad.Abort();
                db.CurrentLoad.Dispose();

                db.CurrentLoad = null;
                db.CurrentLoadFileId = default;
            }
        }

        #endregion // Unload

        #region Retrieval

        /// <summary>
        /// Resolves the path information for the given line code.
        /// </summary>
        static internal VoxFileEntry ResolveEntryForLineCode(VoxDatabase db, StringHash32 lineCode) {
            VoxFileEntry entry;
            if (!db.FileEntryMap.TryGetValue(lineCode, out entry)) {
                using (var psb = PooledStringBuilder.Create()) {
                    psb.Builder.Append(db.BasePath).Append('/');
                    if (!string.IsNullOrEmpty(db.LanguagePath)) {
                        psb.Builder.Append(db.LanguagePath).Append('/');
                    }
                    if (db.LineCodeToReadableFileName.TryGetValue(lineCode, out string readableName)) {
                        psb.Builder.Append(readableName);
                    } else {
                        psb.Builder.Append(lineCode.ToString());
                    }
                    psb.Builder.Append(db.FileExtension);
                    psb.Builder.Replace("//", "/").Replace('\\', '/');

                    entry.Path = psb.Builder.Flush();
                    entry.PathHash = StringHash32.Fast(entry.Path);
                    db.FileEntryMap.Add(lineCode, entry);
                }
            }
            return entry;
        }

        /// <summary>
        /// Attempts to retrieve file data for the given line code.
        /// </summary>
        static public bool TryGetData(StringHash32 lineCode, out VoxFileData data) {
            if (DB.FileEntryMap.TryGetValue(lineCode, out var entry)) {
                return DB.LoadedFileDataMap.TryGetValue(entry.PathHash, out data);
            } else {
                data = default;
                return false;
            }
        }

        #endregion // Retrieval

        #region Emitters

        /// <summary>
        /// Returns the emitter with the given id.
        /// </summary>
        static public VoxEmitter GetEmitter(StringHash32 id) {
            DB.EmitterMap.TryGetValue(id, out VoxEmitter emitter);
            return emitter;
        }

        #endregion // Emitters
    }
}