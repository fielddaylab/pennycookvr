using System;
using System.Collections;
using System.Collections.Generic;
using BeauUtil;
using FieldDay;
using FieldDay.Components;
using FieldDay.Scenes;
using ScriptableBake;
using UnityEngine;

namespace Pennycook {
    public sealed class LODElement : BatchedComponent, IScenePreload, IBaked {
        #region Inspector

        [Header("Components")]
        public Animator Animator;
        [HideIfField("MeshFilter")] public SkinnedMeshRenderer SkinnedMesh;
        [ShowIfField("SkinnedMesh")] public bool SkinnedMeshHasOptimizedBones;
        [HideIfField("SkinnedMesh")] public MeshRenderer MeshRenderer;
        [HideIfField("SkinnedMesh")] public MeshFilter MeshFilter;
        [HideInInspector] public float WorldSize;

        [Header("Configurations")]
        public LODLevelConfig Close = new LODLevelConfig() { ScreenProportion = 0.4f, Skinning = SkinQuality.Bone4 };
        public LODLevelConfig Mid = new LODLevelConfig() { ScreenProportion = 0.3f, Skinning = SkinQuality.Bone2 };
        public LODLevelConfig Far = new LODLevelConfig() { ScreenProportion = 0.1f, Skinning = SkinQuality.Bone1 };

        [Header("Behavior Changes")]
        public ActiveGroup HighDetailGroup;
        public ActiveGroup LowDetailGroup;

        #endregion // Inspector

        [NonSerialized] public Transform CachedTransform;
        [NonSerialized] public LODLevel LastAppliedLevel = LODLevel.Uninitialized;

        public readonly CastableEvent<LODLevel> OnLevelChanged = new CastableEvent<LODLevel>();

        public Renderer Renderer {
            get { return SkinnedMesh ? SkinnedMesh : MeshRenderer; }
        }

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            this.CacheComponent(ref CachedTransform);

            if (Animator) {
                Animator.keepAnimatorStateOnDisable = true;
            }

            return null;
        }

#if UNITY_EDITOR

        private void Reset() {
            SkinnedMesh = GetComponent<SkinnedMeshRenderer>();
            MeshFilter = GetComponent<MeshFilter>();
            MeshRenderer = GetComponent<MeshRenderer>();
            Animator = GetComponentInParent<Animator>();
        }

        int IBaked.Order { get { return 0; } }

        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
            HighDetailGroup.SetActive(false);
            LowDetailGroup.SetActive(true);

            Mesh closeMesh = null;
            Material closeMaterial = null;
            if (SkinnedMesh) {
                closeMesh = SkinnedMesh.sharedMesh;
                closeMaterial = SkinnedMesh.sharedMaterial;
                WorldSize = SkinnedMesh.bounds.size.magnitude;
            } else if (MeshFilter && MeshRenderer) {
                closeMesh = MeshFilter.sharedMesh;
                closeMaterial = MeshRenderer.sharedMaterial;
                WorldSize = MeshRenderer.bounds.size.magnitude;
            }

            Close.Mesh = closeMesh;
            Close.Material = closeMaterial;

            if (!Mid.Mesh) {
                Mid.Mesh = closeMesh;
            }
            if (!Mid.Material) {
                Mid.Material = closeMaterial;
            }

            if (!Far.Mesh) {
                Far.Mesh = Mid.Mesh;
            }
            if (!Far.Material) {
                Far.Material = Mid.Material;
            }

            return true;
        }

#endif // UNITY_EDITOR
    }

    public enum LODLevel {
        Close,
        Mid,
        Far,
        SuperFar,
        Uninitialized = -1
    }

    [Serializable]
    public struct LODLevelConfig {
        [Range(0, 1)] public float ScreenProportion;
        public SkinQuality Skinning;
        public Mesh Mesh;
        public Material Material;
        public bool Cull;
    }
}
