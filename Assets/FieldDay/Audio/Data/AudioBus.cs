using System;
using BeauUtil;
using FieldDay.Assets;

namespace FieldDay.Audio {
    public sealed class AudioBus : NamedAsset {
        [AudioBusId] public StringHash32 ParentId;

        #region Lookup

        static public readonly StringHash32 Master = "Master";

        #endregion // Lookup
    }

    public sealed class AudioBusIdAttribute : AssetNameAttribute {
        public AudioBusIdAttribute() : base(typeof(AudioBus), true) {
            DropdownNullName = "[Master]";
        }
    }
}