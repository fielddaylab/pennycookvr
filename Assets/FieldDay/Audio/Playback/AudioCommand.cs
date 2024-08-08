using System.Runtime.InteropServices;
using BeauUtil;

namespace FieldDay.Audio {
    internal enum AudioCommandType : ushort {
        PlayClipFromName,
        StopWithHandle,
        StopWithTag,
        StopAll,
        SetFloatParameter,
        SetBoolParameter
    }

    #region Unions

    /// <summary>
    /// Reference to a playing instance, or a group.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    internal struct AudioIdRef {
        [FieldOffset(0)] public StringHash32 Id;
        [FieldOffset(0)] public AudioHandle Handle;

        static public implicit operator AudioIdRef(StringHash32 id) {
            return new AudioIdRef() { Id = id };
        }

        static public implicit operator AudioIdRef(AudioHandle handle) {
            return new AudioIdRef() { Handle = handle };
        }
    }

    /// <summary>
    /// Reference to an asset by name, or explicitly as an instance.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    internal struct AudioAssetRef {
        [FieldOffset(0)] public StringHash32 AssetId;
        [FieldOffset(0)] public int InstanceId;

        static public implicit operator AudioAssetRef(StringHash32 id) {
            return new AudioAssetRef() { AssetId = id };
        }

        static public implicit operator AudioAssetRef(UnityEngine.Object obj) {
            return new AudioAssetRef() { InstanceId = UnityHelper.Id(obj) };
        }
    }

    #endregion // Unions

    internal struct AudioCommand {

    }
}