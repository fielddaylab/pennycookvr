using System;
using BeauRoutine.Extensions;
using BeauUtil;
using FieldDay.Assets;
using UnityEngine;

namespace FieldDay.Audio {
    /// <summary>
    /// Audio event information.
    /// </summary>
    [CreateAssetMenu(menuName = "Field Day/Audio/Audio Event")]
    public class AudioEvent : NamedAsset {
        public AudioClip[] Samples = Array.Empty<AudioClip>();

        [Header("Playback Parameters")]
        public FloatRange Volume = new FloatRange(1);
        public FloatRange Pitch = new FloatRange(1);
        public FloatRange Pan = new FloatRange(0);
        public FloatRange Delay = new FloatRange(0);
        [Space]
        public bool Loop;
        public bool RandomizeStartTime;

        [Header("Other Parameters")]
        [Range(0, 1)] public byte Priority = 128;
        [AssetName(typeof(AudioEmitterProfile))] public StringHash32 EmitterConfiguration;

        [NonSerialized] internal StringHash32 CachedId;
        [NonSerialized] internal RandomDeck<AudioClip> SampleSelector;
        
        /// <summary>
        /// Returns if this is a valid event.
        /// </summary>
        public bool IsValid() {
            return Samples.Length > 0;
        }
    }

    /// <summary>
    /// Event reference attribute.
    /// </summary>
    public class AudioEventRefAttribute : AssetNameAttribute {
        public AudioEventRefAttribute() : base(typeof(AudioEvent), true) { }

        protected internal override string Name(UnityEngine.Object obj) {
            return base.Name(obj).Replace('-', '/');
        }
    }
}