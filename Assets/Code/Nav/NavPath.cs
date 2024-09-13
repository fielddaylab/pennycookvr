using UnityEngine;
using BeauUtil;

namespace Pennycook {
    public sealed class NavPath {
        public RingBuffer<Vector3> Positions = new RingBuffer<Vector3>(8, RingBufferMode.Expand);
    }
}