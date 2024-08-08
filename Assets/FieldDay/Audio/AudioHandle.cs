using System;
using System.Runtime.InteropServices;
using BeauUtil;
using UnityEngine;

namespace FieldDay.Audio {
    /// <summary>
    /// Audio playback handle.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 4)]
    public struct AudioHandle {
        [FieldOffset(0)] private readonly UniqueId16 m_Id;

        internal AudioHandle(UniqueId16 id) {
            m_Id = id;
        }
    }
}