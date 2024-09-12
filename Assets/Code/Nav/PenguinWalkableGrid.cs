using System;
using System.Collections;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.Scenes;
using FieldDay.SharedState;
using UnityEngine;

namespace Pennycook {
    public sealed class PenguinWalkableGrid : SharedStateComponent, IRegistrationCallbacks {
        #region Inspector

        public Vector3 Region;
        public float DefaultHeight;

        [Header("Construction Parameters")]
        public float Resolution = 0.1f;
        public LayerMask RaycastMask;
        public float RaycastNormalYThreshold = 0.95f;
        public LayerMask SolidRaycastMask;
        public float SolidRaycastRadius = 0.8f;

        #endregion // Inspector

        [NonSerialized] public UnsafeBitSet WalkableGrid;
        [NonSerialized] public UnsafeSpan<float> WalkableHeight;
        [NonSerialized] public int VoxelCountX;
        [NonSerialized] public int VoxelCountZ;
        [NonSerialized] public Vector3 MinPos;
        [NonSerialized] public AsyncHandle LoadHandle;

#if UNITY_EDITOR
        private void OnDrawGizmos() {
            Gizmos.color = Color.green.WithAlpha(0.5f);
            Gizmos.DrawWireCube(transform.position, Region);
        }
#endif // UNITY_EDITOR

        private unsafe void OnDestroy() {
            WalkableGrid.Unpack(out var bits);
            Unsafe.Free(bits.Ptr);
            Unsafe.Free(WalkableHeight.Ptr);
        }

        #region Voxels

        public bool TryGetVoxel(Vector3 pos, out int voxelIndex) {
            Vector3 adjusted = pos;
            adjusted.x -= MinPos.x;
            adjusted.z -= MinPos.z;

            int x = (int) (adjusted.x / Resolution);
            int z = (int) (adjusted.z / Resolution);

            if (x < 0 || x >= VoxelCountX || z < 0 || z >= VoxelCountZ) {
                voxelIndex = -1;
                return false;
            }

            voxelIndex = x + z * VoxelCountX;
            return true;
        }

        public bool TryGetVoxelXZ(Vector3 pos, out int voxelX, out int voxelZ) {
            Vector3 adjusted = pos;
            adjusted.x -= MinPos.x;
            adjusted.z -= MinPos.z;

            int x = (int) (adjusted.x / Resolution);
            int z = (int) (adjusted.z / Resolution);

            if (x < 0 || x >= VoxelCountX || z < 0 || z >= VoxelCountZ) {
                voxelX = voxelZ = -1;
                return false;
            }

            voxelX = x;
            voxelZ = z;
            return true;
        }

        public bool TryGetPosition(int voxelIdx, out Vector3 pos) {
            if (voxelIdx < 0 || voxelIdx >= (VoxelCountX * VoxelCountZ)) {
                pos = default;
                return false;
            }

            int vX = voxelIdx % VoxelCountX;
            int vZ = voxelIdx / VoxelCountX;
            float x = MinPos.x + (Resolution * (vX + 0.5f));
            float z = MinPos.z + (Resolution * (vZ + 0.5f));

            pos = new Vector3(x, WalkableHeight[voxelIdx], z);
            return true;
        }

        #endregion // Voxels

        #region Walk Analyzer

        private IEnumerator GenerateGridJob() {
            VoxelCountX = Math.Max(1, Mathf.CeilToInt(Region.x / Resolution));
            VoxelCountZ = Math.Max(1, Mathf.CeilToInt(Region.z / Resolution));

            Log.Msg("Voxel Count {0}x{1}={2}", VoxelCountX, VoxelCountZ, VoxelCountX * VoxelCountZ);

            MinPos = transform.position - (Region / 2);

            WalkableGrid = new UnsafeBitSet(Unsafe.AllocSpan<uint>(Unsafe.AlignUp32(VoxelCountX * VoxelCountZ) / 32));
            WalkableHeight = Unsafe.AllocSpan<float>(VoxelCountX * VoxelCountZ);

            Log.Msg("Voxel Grid Total Size={0}", Unsafe.FormatBytes(WalkableGrid.Capacity / 8 + WalkableHeight.Length * 4));

            WalkableGrid.Clear();
            Unsafe.Clear(WalkableHeight);

            for(int i = 0; i < VoxelCountX * VoxelCountZ; i++) {
                TryAddRaycast(i);
                if ((i + 1) % 16 == 0) {
                    yield return null;
                }
            }

            RenderGraph(10);
        }
        
        private void TryAddRaycast(int voxelIdx) {
            Vector3 pos;
            if (!TryGetPosition(voxelIdx, out pos)) {
                return;
            }

            //Log.Msg("raycasting at {0}:{1}", voxelIdx, pos);

            pos.y += 8;
            Ray ray = new Ray(pos, Vector3.down);
            if (!Physics.Raycast(ray, out RaycastHit hit, 16, RaycastMask)) {
                return;
            }

            WalkableHeight[voxelIdx] = hit.point.y;

            bool ignoreNormal = hit.collider.GetComponent<AlwaysWalkable>() != null;

            if (!ignoreNormal && hit.normal.y < RaycastNormalYThreshold) {
                return;
            }

            if (Physics.CheckSphere(hit.point, SolidRaycastRadius, SolidRaycastMask, QueryTriggerInteraction.Collide)) {
                return;
            }

            WalkableGrid.Set(voxelIdx);
        }

        #endregion // Walk Analyzer

        private void RenderGraph(float duration) {
            for (int i = 0; i < VoxelCountX * VoxelCountZ; i++) {
                TryGetPosition(i, out Vector3 pos);
                if (WalkableGrid[i]) {
                    DebugDraw.AddSphere(pos, 0.02f, Color.yellow, duration);
                } else {
                    DebugDraw.AddSphere(pos, 0.02f, Color.red, duration);
                }
            }
        }

        void IRegistrationCallbacks.OnRegister() {
            LoadHandle = Async.Schedule(GenerateGridJob(), AsyncFlags.MainThreadOnly | AsyncFlags.HighPriority);

            Game.Scenes.RegisterLoadDependency(LoadHandle);
        }

        void IRegistrationCallbacks.OnDeregister() {

        }
    }

    static public partial class PenguinUtility {
        [SharedStateReference]
        static public PenguinWalkableGrid WalkGrid { get; private set; }
        
        /// <summary>
        /// Returns if the given position is walkable.
        /// </summary>
        static public bool IsWalkable(Vector3 position) {
            return WalkGrid.TryGetVoxel(position, out int voxelIdx) && WalkGrid.WalkableGrid.IsSet(voxelIdx);
        }

        /// <summary>
        /// Returns if the given position is walkable.
        /// </summary>
        static public bool IsWalkable(ref Vector3 position) {
            if (WalkGrid.TryGetVoxel(position, out int voxelIdx) && WalkGrid.WalkableGrid.IsSet(voxelIdx)) {
                position.y = WalkGrid.WalkableHeight[voxelIdx];
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns the approximate height at the given position.
        /// </summary>
        static public float GetApproximateHeightAt(Vector3 position) {
            if (WalkGrid.TryGetVoxel(position, out int voxelIdx)) {
                return WalkGrid.WalkableHeight[voxelIdx];
            } else {
                return WalkGrid.DefaultHeight;
            }
        }

        /// <summary>
        /// Returns if the given position is on the grid.
        /// </summary>
        static public bool IsOnGrid(Vector3 position) {
            return WalkGrid.TryGetVoxel(position, out var _);
        }
        
        /// <summary>
        /// Returns if all grid positions in a straight line
        /// bewteen the first and second points are walkable.
        /// </summary>
        static public bool IsWalkableRaycast(Vector3 a, Vector3 b) {
            if (!WalkGrid.TryGetVoxelXZ(a, out int aX, out int aZ) || !WalkGrid.TryGetVoxelXZ(b, out int bX, out int bZ)) {
                return false;
            }

            return BresenhamRaycast(aX, aZ, bX, bZ);
        }

        static private bool BresenhamRaycast(int aX, int aZ, int bX, int bZ) {
            // bresenham

            bool transpose = false;
            if (Math.Abs(bX - aX) < Math.Abs(bZ - aZ)) {
                Ref.Swap(ref aX, ref aZ);
                Ref.Swap(ref bX, ref bZ);
                transpose = true;
            }
            if (bX < aX) {
                Ref.Swap(ref aX, ref bX);
                Ref.Swap(ref aZ, ref bZ);
            }

            int dX = bX - aX;
            int dZ = bZ - aZ;

            int stepZ = dZ > 0 ? 1 : -1;

            int dError2 = Math.Abs(dZ) * 2;
            int errorAccum = 0;

            var walkGrid = WalkGrid.WalkableGrid;
            int stride = WalkGrid.VoxelCountX;

            int z = aZ;
            for(int x = aX; x <= bX; x++) {
                int v;
                if (transpose) {
                    v = z + x * stride;
                } else {
                    v = x + z * stride;
                }

                if (!walkGrid.IsSet(v)) {
                    return false;
                }

                errorAccum += dError2;
                if (dError2 > dX) {
                    z += stepZ;
                    errorAccum -= dX * 2;
                }
            }

            return true;
        }
    }
}