using BeauUtil;
using FieldDay.Animation;
using UnityEngine;

namespace Pennycook.Animation {
    public sealed class AdditiveBlendMaskSM : FrameKeyedSMBehaviour {
        [Range(0, 7)] public int LayerIndex;
        [Range(0, 1)] public float DefaultWeight;
        [Range(0, 1)] public float InRangeWeight;
        public OffsetLengthU16[] Ranges;
    }
}