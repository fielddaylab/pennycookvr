#if !UNITY_WEBGL
#define SUPPORTS_AUDIOEFFECTS
#endif // !UNITY_WEBGL

using System;
using System.Runtime.CompilerServices;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace FieldDay.Audio {
    public sealed partial class AudioMgr {
        #region Bus Data

        private unsafe struct BusData {
            public AudioPropertyBlock* ScriptProperties;
            public float ConfigVolume;
            public int ParentIndex;

            public AudioPropertyBlock LastKnownProperties;

            public UniqueId16 Handle;
            public FloatTweenIndices FloatTweens;
        }

        #endregion // Bus Data

        #region Bus Creation

        private int CreateBus(StringHash32 id) {
            if (m_BusCount >= MaxBuses) {
                throw new InvalidOperationException("Maximum number of audio buses created");
            }
        }

        #endregion // Bus Creation

        private ref BusData FindBusForId(StringHash32 id) {
            if (!m_BusNameToIndex.TryGetValue(id.HashValue, out int index)) {
                Log.Error("[AudioMgr] No bus with id '{0}'", id.ToDebugString());
                return ref Unsafe.NullRef<BusData>();
            }

            return ref m_BusData[index];
        }
    }
}