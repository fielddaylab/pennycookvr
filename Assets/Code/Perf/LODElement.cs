using System;
using System.Collections;
using System.Collections.Generic;
using BeauUtil;
using FieldDay.Components;
using FieldDay.Scenes;
using UnityEngine;

namespace Pennycook {
    public sealed class LODElement : BatchedComponent, IScenePreload {
        #region Inspector

        [Header("Components")]
        public Animator Animator;
        public SkinnedMeshRenderer SkinnedMesh;
        public MeshRenderer MeshRenderer;
        public MeshFilter MeshFilter;
        [HideInInspector] public float WorldSize;

        [Header("Configurations")]
        public LODLevelConfig Close = new LODLevelConfig() { ScreenProportion = 0.4f, Skinning = SkinQuality.Bone4 };
        public LODLevelConfig Mid = new LODLevelConfig() { ScreenProportion = 0.3f, Skinning = SkinQuality.Bone2 };
        public LODLevelConfig Far = new LODLevelConfig() { ScreenProportion = 0.1f, Skinning = SkinQuality.Bone1 };

        #endregion // Inspector

        [NonSerialized] public Transform CachedTransform;
        [NonSerialized] public LODLevel LastAppliedLevel;
        [NonSerialized] public AnimatorStateSnapshot CachedAnimatorState;

        public Renderer Renderer {
            get { return SkinnedMesh ? SkinnedMesh : MeshRenderer; }
        }

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            this.CacheComponent(ref CachedTransform);

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

            if (Animator) {
                CachedAnimatorState = new AnimatorStateSnapshot(Animator);
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

#endif // UNITY_EDITOR
    }

    public enum LODLevel {
        Close,
        Mid,
        Far,
        SuperFar
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
