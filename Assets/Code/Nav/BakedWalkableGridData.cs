using UnityEngine;

namespace Pennycook {
    [CreateAssetMenu(menuName = "Pennycook/Baked Walkable Grid Data")]
    public sealed class BakedWalkableGridData : ScriptableObject {
        public ulong GridParamsHash;
        [HideInInspector] public uint[] WalkableGrid;
        [HideInInspector] public uint[] InsideRookeryGrid;
        [HideInInspector] public float[] Heights;
        [HideInInspector] public byte[] Normals;
    }
}