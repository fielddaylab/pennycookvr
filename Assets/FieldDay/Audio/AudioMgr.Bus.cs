#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

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

            int idx = m_BusCount++;
            m_BusNameToIndex.Add(id.HashValue, idx);
            return idx;
        }

        #endregion // Bus Creation

        #region Lookup

        private ref BusData FindBusForId(StringHash32 id) {
            if (!m_BusNameToIndex.TryGetValue(id.HashValue, out int index)) {
                Log.Error("[AudioMgr] No bus with id '{0}'", id.ToDebugString());
                return ref Unsafe.NullRef<BusData>();
            }

            return ref m_BusData[index];
        }

        private int FindBusIndexForId(StringHash32 id) {
            if (!m_BusNameToIndex.TryGetValue(id.HashValue, out int index)) {
                Log.Error("[AudioMgr] No bus with id '{0}'", id.ToDebugString());
                return 0;
            }

            return index;
        }

        #endregion // Lookup

        #region Bindings

        private void ProcessLateBindings() {
            while (m_EventLateBindQueue.TryPopFront(out AudioEvent evt)) {
                evt.CachedBusIndex = FindBusIndexForId(evt.Bus);
            }
        }

        #endregion // Bindings

        private unsafe void UpdateBuses() {
            AudioPropertyBlock block;
            for(int i = 0; i < m_BusCount; i++) {
                ref BusData bus = ref m_BusData[i];
                block = bus.ParentIndex < 0 ? AudioPropertyBlock.Default : m_BusData[bus.ParentIndex].LastKnownProperties;
                AudioPropertyBlock.Combine(block, *bus.ScriptProperties, ref block);
#if DEVELOPMENT
                AudioPropertyBlock.Combine(block, m_DebugBusProperties[i], ref block);
#endif // DEVELOPMENT
                block.Volume *= bus.ConfigVolume;
                bus.LastKnownProperties = block;
            }
        }
    }
}