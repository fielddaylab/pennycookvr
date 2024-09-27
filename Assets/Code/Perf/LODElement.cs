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

        public Animator Animator;
        public SkinnedMeshRenderer SkinnedMesh;
        public MeshRenderer MeshRenderer;
        public MeshFilter MeshFilter;

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
            if (SkinnedMesh) {
                closeMesh = SkinnedMesh.sharedMesh;
            } else if (MeshFilter) {
                closeMesh = MeshFilter.sharedMesh;
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
}
