using System;
using System.Collections;
using System.Runtime.CompilerServices;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.SharedState;
using UnityEngine;
using static BeauData.Serializer;
using static UnityEditor.PlayerSettings;

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
        [NonSerialized] public UnsafeSpan<float> Height;
        [NonSerialized] public UnsafeSpan<float> Normal;
        [NonSerialized] public NavRegionGrid GridParams;
        [NonSerialized] public AsyncHandle LoadHandle;

#if UNITY_EDITOR
        private void OnDrawGizmos() {
            Gizmos.color = Color.green.WithAlpha(0.5f);
            Gizmos.DrawWireCube(transform.position, Region);
        }
#endif // UNITY_EDITOR

        public void RenderGraph(float duration) {
            for (int i = 0; i < GridParams.Count; i++) {
                GridParams.TryGetPosition(i, Height, out Vector3 pos);
                if (WalkableGrid[i]) {
                    DebugDraw.AddSphere(pos, 0.02f, Color.yellow, duration);
                } else {
                    DebugDraw.AddSphere(pos, 0.02f, Color.red, duration);
                }
            }
        }

        void IRegistrationCallbacks.OnRegister() {
            LoadHandle = Async.Schedule(PenguinWalkableGridGenerator.GenerateGridJob(this), AsyncFlags.MainThreadOnly | AsyncFlags.HighPriority);

            Game.Scenes.RegisterLoadDependency(LoadHandle);
        }

        void IRegistrationCallbacks.OnDeregister() {

        }
    }

    public readonly struct NavRegionGrid {
        public readonly Vector3 MinPos;
        public readonly Vector3 Size;
        public readonly int CountX;
        public readonly int CountZ;
        public readonly float Resolution;

        public NavRegionGrid(Vector3 offset, Vector3 size, float resolution) {
            MinPos = offset - size / 2;
            Size = size;
            Resolution = resolution;
            CountX = Math.Max(1, Mathf.CeilToInt(size.x / resolution));
            CountZ = Math.Max(1, Mathf.CeilToInt(size.z / resolution));
        }

        public int Count {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return CountX * CountZ; }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetIndex(int x, int z) {
            return x + z * CountX;
        }

        #region Voxels

        public bool TryGetVoxel(Vector3 pos, out int voxelIndex) {
            Vector3 adjusted = pos;
            adjusted.x -= MinPos.x;
            adjusted.z -= MinPos.z;

            int x = (int) (adjusted.x / Resolution);
            int z = (int) (adjusted.z / Resolution);

            if (x < 0 || x >= CountX || z < 0 || z >= CountZ) {
                voxelIndex = -1;
                return false;
            }

            voxelIndex = x + z * CountX;
            return true;
        }

        public bool TryGetVoxelXZ(Vector3 pos, out int voxelX, out int voxelZ) {
            Vector3 adjusted = pos;
            adjusted.x -= MinPos.x;
            adjusted.z -= MinPos.z;

            int x = (int) (adjusted.x / Resolution);
            int z = (int) (adjusted.z / Resolution);

            if (x < 0 || x >= CountX || z < 0 || z >= CountZ) {
                voxelX = voxelZ = -1;
                return false;
            }

            voxelX = x;
            voxelZ = z;
            return true;
        }

        public bool TryGetPosition(int voxelIdx, float height, out Vector3 pos) {
            if (voxelIdx < 0 || voxelIdx >= (CountX * CountZ)) {
                pos = default;
                return false;
            }

            int vX = voxelIdx % CountX;
            int vZ = voxelIdx / CountX;
            float x = MinPos.x + (Resolution * (vX + 0.5f));
            float z = MinPos.z + (Resolution * (vZ + 0.5f));

            pos = new Vector3(x, height, z);
            return true;
        }

        public bool TryGetPosition(int voxelIdx, UnsafeSpan<float> heights, out Vector3 pos) {
            if (voxelIdx < 0 || voxelIdx >= (CountX * CountZ)) {
                pos = default;
                return false;
            }

            int vX = voxelIdx % CountX;
            int vZ = voxelIdx / CountX;
            float x = MinPos.x + (Resolution * (vX + 0.5f));
            float z = MinPos.z + (Resolution * (vZ + 0.5f));

            pos = new Vector3(x, heights[voxelIdx], z);
            return true;
        }

        #endregion // Voxels
    
        public NavRegionGrid Reslice(float newResolution) {
            return new NavRegionGrid(MinPos + Size / 2, Size, newResolution);
        }
    }

    static internal class PenguinWalkableGridGenerator {
        #region Walk Analyzer

        static internal IEnumerator GenerateGridJob(PenguinWalkableGrid grid) {
            using (Profiling.Time("generating walkable grid", ProfileTimeUnits.Microseconds)) {
                grid.GridParams = new NavRegionGrid(grid.transform.position, grid.Region, grid.Resolution);
                
                Log.Msg("Voxel Count {0}x{1}={2}", grid.GridParams.CountX, grid.GridParams.CountZ, grid.GridParams.Count);

                grid.WalkableGrid = NavMemory.CreateBitGrid(grid.GridParams.CountX, grid.GridParams.CountZ);
                grid.Height = NavMemory.CreateGrid<float>(grid.GridParams.CountX, grid.GridParams.CountZ);
                grid.Normal = NavMemory.CreateGrid<float>(grid.GridParams.CountX, grid.GridParams.CountZ);

                Log.Msg("Voxel Grid Total Size={0}", Unsafe.FormatBytes(grid.WalkableGrid.Capacity / 8 + grid.Height.Length * 4));

                grid.WalkableGrid.Clear();
                Unsafe.Clear(grid.Height);

                for (int i = 0; i < grid.GridParams.Count; i++) {
                    TryAddRaycast(grid, i);
                    if ((i + 1) % 16 == 0) {
                        yield return null;
                    }
                }
            }

            //grid.RenderGraph(60);
        }

        static private void TryAddRaycast(PenguinWalkableGrid grid, int voxelIdx) {
            Vector3 pos;
            if (!grid.GridParams.TryGetPosition(voxelIdx, grid.DefaultHeight, out pos)) {
                return;
            }

            //Log.Msg("raycasting at {0}:{1}", voxelIdx, pos);

            pos.y += 8;
            Ray ray = new Ray(pos, Vector3.down);
            if (!Physics.Raycast(ray, out RaycastHit hit, 16, grid.RaycastMask)) {
                return;
            }

            grid.Height[voxelIdx] = hit.point.y;
            grid.Normal[voxelIdx] = hit.normal.y;

            bool ignoreNormal = hit.collider.GetComponent<AlwaysWalkable>() != null;

            if (!ignoreNormal && hit.normal.y < grid.RaycastNormalYThreshold) {
                return;
            }

            if (Physics.CheckSphere(hit.point, grid.SolidRaycastRadius, grid.SolidRaycastMask, QueryTriggerInteraction.Collide)) {
                return;
            }

            if (Physics.CheckSphere(hit.point + Vector3.up, grid.SolidRaycastRadius, grid.SolidRaycastMask, QueryTriggerInteraction.Collide)) {
                return;
            }

            grid.WalkableGrid.Set(voxelIdx);
        }

        #endregion // Walk Analyzer
    }

    static public partial class PenguinNav {
        [SharedStateReference]
        static public PenguinWalkableGrid WalkGrid { get; private set; }
        
        /// <summary>
        /// Returns if the given position is walkable.
        /// </summary>
        static public bool IsWalkable(Vector3 position) {
            return WalkGrid.GridParams.TryGetVoxel(position, out int voxelIdx) && WalkGrid.WalkableGrid.IsSet(voxelIdx);
        }

        /// <summary>
        /// Returns if the given position is walkable.
        /// </summary>
        static public bool IsWalkable(ref Vector3 position) {
            if (WalkGrid.GridParams.TryGetVoxel(position, out int voxelIdx) && WalkGrid.WalkableGrid.IsSet(voxelIdx)) {
                position.y = WalkGrid.Height[voxelIdx];
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns the approximate height at the given position.
        /// </summary>
        static public float GetApproximateHeightAt(Vector3 position) {
            if (WalkGrid.GridParams.TryGetVoxel(position, out int voxelIdx)) {
                return WalkGrid.Height[voxelIdx];
            } else {
                return WalkGrid.DefaultHeight;
            }
        }

        /// <summary>
        /// Returns accurate height information at the given position.
        /// </summary>
        static public bool GetAccurateTerrainAt(Vector3 position, out float y, out float normal) {
            position.y += 8;
            Ray ray = new Ray(position, Vector3.down);
            if (!Physics.Raycast(ray, out RaycastHit hit, 16, WalkGrid.RaycastMask)) {
                y = WalkGrid.DefaultHeight;
                normal = 1;
                return false;
            }

            y = hit.point.y;
            normal = hit.normal.y;
            return true;
        }

        /// <summary>
        /// Returns the approximate normal y at the given position.
        /// </summary>
        static public float GetApproximateNormalAt(Vector3 position) {
            if (WalkGrid.GridParams.TryGetVoxel(position, out int voxelIdx)) {
                return WalkGrid.Normal[voxelIdx];
            } else {
                return 1;
            }
        }

        /// <summary>
        /// Snaps the given position to approximate ground.
        /// </summary>
        static public Vector3 SnapPositionToApproximateGround(Vector3 position) {
            if (WalkGrid.GridParams.TryGetVoxel(position, out int voxelIdx)) {
                position.y = WalkGrid.Height[voxelIdx];
            }
            return position;
        }

        /// <summary>
        /// Snaps the given position to accurateground.
        /// </summary>
        static public Vector3 SnapPositionToAccurateGround(Vector3 position) {
            Vector3 rayPoint = position;
            rayPoint.y += 8;
            Ray ray = new Ray(rayPoint, Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit, 16, WalkGrid.RaycastMask)) {
                position.y = hit.point.y;
            }
            return position;
        }

        /// <summary>
        /// Returns if the given position is on the grid.
        /// </summary>
        static public bool IsOnGrid(Vector3 position) {
            return WalkGrid.GridParams.TryGetVoxel(position, out var _);
        }
        
        /// <summary>
        /// Returns if all grid positions in a straight line
        /// bewteen the first and second points are walkable.
        /// </summary>
        static public bool IsWalkableRaycast(Vector3 a, Vector3 b) {
            bool onGridA = WalkGrid.GridParams.TryGetVoxelXZ(a, out int aX, out int aZ);
            bool onGridB = WalkGrid.GridParams.TryGetVoxelXZ(b, out int bX, out int bZ);
            if (onGridA != onGridB) {
                return false;
            }

            // if both are off the grid, it's automatically considered walkable.
            if (!onGridA) {
                return true;
            }

            if (aX == bX && aZ == bZ) {
                return WalkGrid.WalkableGrid.IsSet(aX + aZ * WalkGrid.GridParams.CountX);
            }

            return BresenhamRaycast(aX, aZ, bX, bZ);
        }

        static private bool BresenhamRaycast(int x0, int z0, int x1, int z1) {
            // bresenham

            bool transpose = false;
            if (Math.Abs(x0 - x1) < Math.Abs(z0 - z1)) {
                Ref.Swap(ref x0, ref z0);
                Ref.Swap(ref x1, ref z1);
                transpose = true;
            }
            if (x1 < x0) {
                Ref.Swap(ref x0, ref x1);
                Ref.Swap(ref z0, ref z1);
            }

            int dX = x1 - x0;
            int dZ = z1 - z0;

            int stepZ = dZ > 0 ? 1 : -1;

            int dError2 = Math.Abs(dZ) * 2;
            int errorAccum = 0;

            var walkGrid = WalkGrid.WalkableGrid;
            var gridMath = WalkGrid.GridParams;
            
            int z = z0;
            for(int x = x0; x <= x1; x++) {
                int v;
                if (transpose) {
                    v = gridMath.GetIndex(z, x);
                } else {
                    v = gridMath.GetIndex(x, z);
                }

                if (!walkGrid.IsSet(v)) {
                    return false;
                }

                errorAccum += dError2;
                if (errorAccum > dX) { 
                    z += stepZ;
                    errorAccum -= dX * 2;
                }
            }

            return true;
        }
    }
}