//#define FORCE_USE_BAKED_ASSET

using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Assets;
using FieldDay.Debugging;
using FieldDay.SharedState;
using ScriptableBake;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;

namespace Pennycook {
    public sealed class PenguinWalkableGrid : SharedStateComponent, IRegistrationCallbacks, IBaked {
        #region Inspector

        public Vector3 Region;
        public float DefaultHeight;
        public Collider WithinRookeryCollider;
        public BakedWalkableGridData BakedData;

        [Header("Construction Parameters")]
        public float Resolution = 0.1f;
        public LayerMask RaycastMask;
        public float RaycastNormalYThreshold = 0.95f;
        public LayerMask SolidRaycastMask;
        public float SolidRaycastRadius = 0.8f;

        #endregion // Inspector

        [NonSerialized] public UnsafeBitSet WalkableGrid;
        [NonSerialized] public UnsafeBitSet InsideRookeryGrid;
        [NonSerialized] public UnsafeSpan<float> Height;
        [NonSerialized] public UnsafeSpan<Fraction8> Normal;
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
                bool walkable = WalkableGrid[i];
                bool insideRookery = InsideRookeryGrid[i];
                if (walkable) {
                    if (insideRookery) {
                        DebugDraw.AddSphere(pos, 0.02f, Color.yellow, duration);
                    } else {
                        DebugDraw.AddSphere(pos, 0.02f, ColorBank.DarkOrange, duration);
                    }
                } else {
                    if (insideRookery) {
                        DebugDraw.AddSphere(pos, 0.02f, Color.red, duration);
                    } else {
                        DebugDraw.AddSphere(pos, 0.02f, Color.black, duration);
                    }
                }
            }
        }

        void IRegistrationCallbacks.OnRegister() {
            LoadHandle = Async.Schedule(PenguinWalkableGridGenerator.GenerateGridJob(this), AsyncFlags.MainThreadOnly | AsyncFlags.HighPriority);

            Game.Scenes.RegisterLoadDependency(LoadHandle);
        }

        void IRegistrationCallbacks.OnDeregister() {

        }

#if UNITY_EDITOR

        int IBaked.Order { get { return 1000; } }

        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
            if ((flags & BakeFlags.IsBuild) != 0) {
                Baking.Destroy(WithinRookeryCollider.gameObject);
                WithinRookeryCollider = null;
                return true;
            }

            return false;
        }

#endif // UNITY_EDITOR
    }

    [StructLayout(LayoutKind.Explicit)]
    public readonly struct NavRegionGrid {
        [FieldOffset(0)] private readonly ulong _Align;
        [FieldOffset(0)] public readonly Vector3 MinPos;
        [FieldOffset(12)] public readonly Vector3 Size;
        [FieldOffset(24)] public readonly int CountX;
        [FieldOffset(28)] public readonly int CountZ;
        [FieldOffset(32)] public readonly float Resolution;

        public NavRegionGrid(Vector3 offset, Vector3 size, float resolution) {
            _Align = 0;
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
            using (Profiling.Time("generating walkable grid", ProfileTimeUnits.Milliseconds)) {
                grid.GridParams = new NavRegionGrid(grid.transform.position, grid.Region, grid.Resolution);
                
                Log.Msg("Voxel Count {0}x{1}={2}", grid.GridParams.CountX, grid.GridParams.CountZ, grid.GridParams.Count);

                grid.WalkableGrid = NavMemory.CreateBitGrid(grid.GridParams.CountX, grid.GridParams.CountZ);
                grid.InsideRookeryGrid = NavMemory.CreateBitGrid(grid.GridParams.CountX, grid.GridParams.CountZ);
                grid.Height = NavMemory.CreateGrid<float>(grid.GridParams.CountX, grid.GridParams.CountZ);
                grid.Normal = NavMemory.CreateGrid<Fraction8>(grid.GridParams.CountX, grid.GridParams.CountZ);

                Log.Msg("Voxel Grid Total Size={0}", Unsafe.FormatBytes(grid.WalkableGrid.Capacity / 8 + grid.InsideRookeryGrid.Capacity / 8 + grid.Height.Length * 4 + grid.Normal.Length * 4));

                ulong paramHash = Unsafe.Hash64(grid.GridParams);

                bool hadBaked;
#if !UNITY_EDITOR || FORCE_USE_BAKED_ASSET
                if (grid.BakedData != null) {
                    if (grid.BakedData.GridParamsHash != paramHash) {
                        Log.Error("[PenguinWalkableGrid] Baked data was not generated with the same grid parameters - not using");
                        hadBaked = false;
                    } else {
                        hadBaked = true;
                    }
                } else {
                    hadBaked = false;
                }
#else
                hadBaked = false;
#endif // !UNITY_EDITOR

                if (hadBaked) {
                    Log.Msg("[PenguinWalkableGrid] Using baked data");

                    CopyBakedBitGrid(grid.BakedData.WalkableGrid, grid.WalkableGrid);
                    Log.Msg("[PenguinWalkableGrid] Copied walkable grid");
                    yield return null;
                    CopyBakedBitGrid(grid.BakedData.InsideRookeryGrid, grid.InsideRookeryGrid);
                    Log.Msg("[PenguinWalkableGrid] Copied inside rookery grid");
                    yield return null;
                    CopyBakedData(grid.BakedData.Heights, grid.Height);
                    Log.Msg("[PenguinWalkableGrid] Copied height grid");
                    yield return null;
                    CopyBakedData(grid.BakedData.Normals, grid.Normal);
                    Log.Msg("[PenguinWalkableGrid] Copied normals grid");
                    yield return null;

                    grid.BakedData = null;
                    AssetUtility.ManualUnload(grid.BakedData);
                } else {
                    grid.WalkableGrid.Clear();
                    Unsafe.Clear(grid.Height);

                    int execStride = grid.GridParams.CountX * 5;
                    for (int i = 0; i < grid.GridParams.Count; i++) {
                        TryAddRaycast(grid, i);
                        if ((i + 1) % execStride == 0) {
                            yield return null;
                        }
                    }
                }

#if UNITY_EDITOR
                if (!hadBaked) {
                    CopyDataToBakedData(grid, paramHash, grid.BakedData);
                }
#endif // UNITY_EDITOR
            }

            UnityHelper.SafeDestroyGO(ref grid.WithinRookeryCollider);

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
            grid.Normal[voxelIdx] = new Fraction8(hit.normal.y);

            if (grid.WithinRookeryCollider.Raycast(ray, out var tempHit, 16)) {
                grid.InsideRookeryGrid.Set(voxelIdx);
            }

            bool ignoreNormal = hit.collider.GetComponent<AlwaysWalkable>() != null;

            if (!ignoreNormal && hit.normal.y < grid.RaycastNormalYThreshold) {
                return;
            }

            if (Physics.CheckCapsule(hit.point, hit.point + Vector3.up, grid.SolidRaycastRadius, grid.SolidRaycastMask, QueryTriggerInteraction.Collide)) {
                return;
            }

            grid.WalkableGrid.Set(voxelIdx);
        }

        static private unsafe void CopyBakedData<T, U>(T[] fromAsset, UnsafeSpan<U> destination) where T : unmanaged where U : unmanaged {
            Assert.True(destination.ByteLength == fromAsset.Length * sizeof(T));
            fixed(T* fromPtr = fromAsset) {
                Unsafe.Copy(fromPtr, fromAsset.Length * sizeof(T), destination.Ptr);
            }
        }

        static private unsafe void CopyBakedBitGrid(uint[] fromAsset, UnsafeBitSet destination) {
            destination.Unpack(out UnsafeSpan<uint> dest);
            Assert.True(dest.Length == fromAsset.Length);
            fixed(uint* fromPtr = fromAsset) {
                Unsafe.Copy(fromPtr, fromAsset.Length * sizeof(uint), dest.Ptr);
            }
        }

        #endregion // Walk Analyzer

#if UNITY_EDITOR

        static private unsafe void CopyDataToBakedData(PenguinWalkableGrid grid, ulong paramHash, BakedWalkableGridData asset) {
            if (asset == null) {
                return;
            }

            asset.GridParamsHash = paramHash;
            CopyBitGrid(grid.WalkableGrid, ref asset.WalkableGrid);
            CopyBitGrid(grid.InsideRookeryGrid, ref asset.InsideRookeryGrid);
            CopySpan(grid.Height, ref asset.Heights);
            CopySpanAsBytes(grid.Normal, ref asset.Normals);

            Baking.SetDirty(asset);
            Log.Msg("[PenguinWalkableGrid] Copied data to asset");
        }

        static private unsafe void CopyBitGrid(UnsafeBitSet bits, ref uint[] asset) {
            bits.Unpack(out UnsafeSpan<uint> span);
            asset = new uint[span.Length];
            Unsafe.CopyArray(span.Ptr, span.Length, asset);
        }

        static private unsafe void CopySpan<T>(UnsafeSpan<T> data, ref T[] asset) where T : unmanaged {
            asset = new T[data.Length];
            Unsafe.CopyArray(data.Ptr, data.Length, asset);
        }

        static private unsafe void CopySpanAsBytes<T>(UnsafeSpan<T> data, ref byte[] asset) where T : unmanaged {
            asset = new byte[data.ByteLength];
            fixed(byte* assetBytes = asset) {
                Unsafe.Copy(data.Ptr, data.ByteLength, assetBytes);
            }
        }

#endif // UNITY_EDITOR
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
        /// Returns if the given position is within the rookery.
        /// </summary>
        static public bool IsWithinRookery(Vector3 position) {
            return WalkGrid.GridParams.TryGetVoxel(position, out int voxelIdx) && WalkGrid.InsideRookeryGrid.IsSet(voxelIdx);
        }

        /// <summary>
        /// Returns if the given position is walkable and within the rookery.
        /// </summary>
        static public bool IsWalkableWithinRookery(Vector3 position) {
            return WalkGrid.GridParams.TryGetVoxel(position, out int voxelIdx) && WalkGrid.WalkableGrid.IsSet(voxelIdx) && WalkGrid.InsideRookeryGrid.IsSet(voxelIdx);
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

        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
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