using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Audio;
using FieldDay.SharedState;
using FieldDay.Systems;

namespace FieldDay.Vox {
    [SysUpdate(GameLoopPhaseMask.PreUpdate | GameLoopPhaseMask.UnscaledUpdate | GameLoopPhaseMask.UnscaledLateUpdate, 1001)]
    internal class VoxRequestSystem : ISystem {
        public bool HasWork() {
            return VoxUtility.Requests != null;
        }

        public void ProcessWork(float deltaTime) {
            var requestBuffer = VoxUtility.Requests.ActiveRequests;
            for(int i = requestBuffer.Count - 1; i >= 0; i--) {
                ref VoxRequest req = ref requestBuffer[i];
                switch (req.State) {
                    case VoxRequestPlaybackState.Queued: {
                        SetupInternalData(ref req);

                        if (req.Type == VoxType.Event) {

                        } else if (!CheckLoaded(ref req)) {
                            VoxUtility.PushImmediateLoad(req.LineCode);
                            req.State = VoxRequestPlaybackState.Loading;
                        }
                        break;
                    }

                    case VoxRequestPlaybackState.Loading: {
                        CheckLoaded(ref req);
                        break;
                    }

                    case VoxRequestPlaybackState.Playing: {
                        if (!Sfx.IsActive(req.Emitter.AudioHandle)) {
                            Log.Debug("[VoxRequestSystem] Playback from VoxEmitter '{0}' completed", GetDebugName(req.Emitter));
                            req.State = VoxRequestPlaybackState.Complete;
                        }
                        break;
                    }
                }

                if (req.State == VoxRequestPlaybackState.Complete) {
                    VoxUtility.KillVoxRequestAtIndex(ref req, i);
                }
            }
        }

        static private void SetupInternalData(ref VoxRequest req) {
            switch (req.Type) {
                case VoxType.Vox: {
                    req.ExposedCode = req.LineCode;
                    break;
                }

                case VoxType.Event: {
                    req.ExposedCode = req.AudioEvent;
                    break;
                }
            }
        }

        static private bool CheckLoaded(ref VoxRequest req) {
            if (VoxUtility.TryGetData(req.LineCode, out VoxFileData file)) {
                if (file.Clip == null) {
                    Log.Warn("[VoxRequestSystem] No clip loaded for line '{0}' - skipping voice playback from VoxEmitter '{1}'", req.LineCode, GetDebugName(req.Emitter));
                    req.State = VoxRequestPlaybackState.Complete;
                } else {
                    req.State = VoxRequestPlaybackState.Playing;

                    StringHash32 eventId = req.AudioEvent.IsEmpty ? req.Emitter.DefaultPlaybackEvent : req.AudioEvent;
                    req.Emitter.AudioHandle = Sfx.PlayFrom(eventId, file.Clip, req.Emitter.Player);
                    Log.Debug("[VoxRequestSystem] Playing clip '{0}' from VoxEmitter '{1}'", file.Clip.name, GetDebugName(req.Emitter));

                    string subtitle = req.SubtitleFallback; // TODO: use subtitle from file

                    VoxRequest cachedReq = req;

                    if (VoxUtility.Requests.OnAnyVoiceSpeak != null) {
                        VoxUtility.Requests.OnAnyVoiceSpeak(cachedReq.Emitter, cachedReq.ExposedCode, cachedReq.Type);
                    }

                    cachedReq.Emitter.OnStartSpeaking.Invoke(cachedReq.ExposedCode, cachedReq.Type);

                    if (cachedReq.OnSpeak != null) {
                        cachedReq.OnSpeak(cachedReq.Emitter, cachedReq.ExposedCode, cachedReq.Type);
                    }
                }

                return true;
            }

            return false;
        }

        static private string GetDebugName(VoxEmitter emitter) {
            if (emitter.CharacterId.IsEmpty) {
                return emitter.gameObject.name;
            } else {
                return emitter.CharacterId.ToDebugString();
            }
        }

        public void Initialize() {
        }

        public void Shutdown() {
        }
    }
}