using FieldDay.Assets;
using UnityEngine;

namespace FieldDay.Audio {
    /// <summary>
    /// Audio emitter profile.
    /// </summary>
    [CreateAssetMenu(menuName = "Field Day/Audio/Audio Emitter Profile")]
    public class AudioEmitterProfile : NamedAsset {
        public AudioEmitterConfig Config;
    }
}