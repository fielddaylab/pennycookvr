using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Audio;
using FieldDay.SharedState;
using FieldDay.Systems;

namespace FieldDay.Vox {
    public struct VoxRequest {
        public StringHash32 LineCode;
        public StringHash32 AudioEvent;
        public string SubtitleFallback;
        
        public VoxPriority Priority;
        public VoxType Type;
        public bool UnloadAfterPlayback;

        public VoxEventDelegate OnSpeak;
        public VoxEventDelegate OnFinish;

        internal VoxEmitter Emitter;
        internal StringHash32 ExposedCode;
        internal VoxRequestPlaybackState State;
        internal UniqueId16 Id;
    }

    public delegate void VoxEventDelegate(VoxEmitter emitter, StringHash32 code, VoxType type);

    public enum VoxPriority : byte {
        Unassigned = 0,
        Effort,
        Chatter,
        Cutscene,
        Reaction,
        Highest
    }

    public enum VoxType : byte {
        Event,
        Vox
    }

    internal class VoxRequestState : ISharedState {
        public RingBuffer<VoxRequest> ActiveRequests = new RingBuffer<VoxRequest>(8);
        public UniqueIdAllocator16 RequestIdGenerator = new UniqueIdAllocator16(32);

        public VoxEventDelegate OnAnyVoiceSpeak;
        public VoxEventDelegate OnAnyVoiceFinished;
    }

    internal enum VoxRequestPlaybackState {
        Queued,
        Loading,
        EventLoading,
        Playing,
        Complete
    }

    static public partial class VoxUtility {
        [SharedStateReference] static internal VoxRequestState Requests { get; private set; }

        static internal bool KillVoxRequest(ref VoxRequestHandle id) {
            if (!Requests.RequestIdGenerator.IsValid(id.m_Id)) {
                id = default;
                return false;
            }

            var requestBuffer = Requests.ActiveRequests;
            for (int i = requestBuffer.Count - 1; i >= 0; i--) {
                ref VoxRequest req = ref requestBuffer[i];
                if (req.Id != id.m_Id) {
                    continue;
                }

                KillVoxRequestAtIndex(ref req, i);
                id = default;
                return true;
            }

            return false;
        }

        static internal void KillVoxRequestAtIndex(ref VoxRequest req, int index) {
            VoxEmitter emitter = req.Emitter;
            Sfx.Stop(emitter.AudioHandle);
            emitter.AudioHandle = default;

            emitter.Player.Stop();
            emitter.PlayingPriority = VoxPriority.Unassigned;

            if (req.UnloadAfterPlayback) {
                QueueUnload(req.LineCode);
            }

            VoxRequest cachedReq = req;

            Requests.ActiveRequests.FastRemoveAt(index);
            Requests.RequestIdGenerator.Free(req.Id);
            req = default;

            if (Requests.OnAnyVoiceFinished != null) {
                Requests.OnAnyVoiceFinished(cachedReq.Emitter, cachedReq.ExposedCode, cachedReq.Type);
            }

            cachedReq.Emitter.OnFinishSpeaking.Invoke(cachedReq.ExposedCode, cachedReq.Type);

            if (cachedReq.OnFinish != null) {
                cachedReq.OnFinish(cachedReq.Emitter, cachedReq.ExposedCode, cachedReq.Type);
            }
        }

        static public VoxRequestHandle Speak(VoxEmitter emitter, StringHash32 lineCode) {
            Assert.True(emitter != null);
            // TODO: Implement
            return default;
        }

        static public VoxRequestHandle Speak(StringHash32 emitterId, StringHash32 lineCode) {
            VoxEmitter emitter = GetEmitter(emitterId);
            if (emitter == null) {
                Log.Warn("[VoxUtility] No VoxEmitter with id '{0}'", emitterId);
                return default;
            }

            return Speak(emitter, lineCode);
        }
    }
}