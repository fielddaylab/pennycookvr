using System;
using System.Collections;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Components;
using FieldDay.Scenes;
using FieldDay.Systems;
using UnityEngine;
using UnityEngine.Rendering;

namespace Pennycook {
    [SysUpdate(GameLoopPhase.UnscaledLateUpdate)]
    public sealed class LODEvalSystem : ComponentSystemBehaviour<LODElement> {
        public unsafe override void ProcessWork(float deltaTime) {

            var references = Find.Components<LODReferenceCamera>();
            if (references.Count <= 0) {
                return;
            }

            ReferenceData* refData = stackalloc ReferenceData[references.Count];
            int activeCameras = 0;

            foreach (var refCam in references) {
                bool cameraEnabled = refCam.CachedCamera.enabled || (refCam.RefreshRate && !refCam.RefreshRate.Paused);
                if (cameraEnabled) {
                    ref ReferenceData refDatum = ref refData[activeCameras];
                    refCam.CachedTransform.GetPositionAndRotation(out refDatum.Position, out Quaternion rot);
                    refDatum.Forward = Geom.Forward(rot);
                    refDatum.UnitFrustum = 2f * Mathf.Tan(refCam.CachedCamera.fieldOfView / 2 * Mathf.Deg2Rad);
                    refDatum.Aspect = refCam.CachedCamera.aspect;
                    activeCameras++;
                }
            }

            if (activeCameras <= 0) {
                return;
            }

            int elemIdx = 0;
            foreach(var element in m_Components) {
                if (!Frame.Interval(4, elemIdx++)) {
                    continue;
                }

                if (element.Animator && !element.Animator.isInitialized) {
                    continue;
                }

                GetHeightAndLook(element, refData, activeCameras, out float bestHeight, out float bestLook);

                LODLevel level;
                if (bestHeight >= element.Close.ScreenProportion) {
                    level = LODLevel.Close;
                } else if (bestHeight >= element.Mid.ScreenProportion) {
                    level = LODLevel.Mid;
                } else { // if (bestHeight >= element.Far.ScreenProportion) {
                    level = LODLevel.Far;
                } // else {
                //    level = LODLevel.SuperFar;
                //}

                LODLevelConfig levelConfig = GetConfigForLevel(element, level);

                Renderer r = element.Renderer;
                r.enabled = level == LODLevel.Close || (bestLook > -0.2f && !levelConfig.Cull);

                if (level != element.LastAppliedLevel) {
                    element.LastAppliedLevel = level;

                    if (element.SkinnedMesh) {
                        element.SkinnedMesh.quality = levelConfig.Skinning;
                        element.SkinnedMesh.sharedMesh = levelConfig.Mesh;
                        element.SkinnedMesh.receiveShadows = level == LODLevel.Close;
                        element.SkinnedMesh.sharedMaterial = levelConfig.Material;
                    } else if (element.MeshFilter) {
                        element.MeshFilter.sharedMesh = levelConfig.Mesh;
                        element.MeshRenderer.receiveShadows = level == LODLevel.Close;
                        element.MeshRenderer.sharedMaterial = levelConfig.Material;
                    }

                    r.shadowCastingMode = level < LODLevel.Far ? ShadowCastingMode.On : ShadowCastingMode.Off;

                    if (element.Animator) {
                        bool animating = element.Animator.enabled;
                        if (animating != !levelConfig.Cull) {
                            if (animating) {
                                element.CachedAnimatorState.Read(element.Animator);
                                element.Animator.enabled = false;
                            } else {
                                element.Animator.enabled = true;
                                element.CachedAnimatorState.Write(element.Animator);
                            }
                        }
                    }

                    Log.Msg("[LODEvalSystem] Element '{0}' transitioned to lod{1}", element.gameObject.name, (int) level);
                }
            }
        }

        private struct ReferenceData {
            public Vector3 Position;
            public Vector3 Forward;
            public float UnitFrustum;
            public float Aspect;
        }

        static private unsafe void GetHeightAndLook(LODElement element, ReferenceData* refData, int refCount, out float height, out float look) {
            Vector3 pos = element.CachedTransform.position;
            float maxHeight = 0;
            float bestLook = -1;
            for(int i = 0; i < refCount; i++) {
                ReferenceData refDatum = refData[i];
                Vector3 vec = pos - refDatum.Position;
                Vector3 lookVec = vec.normalized;
                float distXZ = Mathf.Sqrt(vec.x * vec.x + vec.z * vec.z);
                float heightAtDist = refDatum.UnitFrustum * distXZ;
                maxHeight = Math.Max(maxHeight, element.WorldSize / heightAtDist);
                bestLook = Math.Max(bestLook, Vector3.Dot(lookVec, refDatum.Forward));
            }

            height = maxHeight;
            look = bestLook;
        }

        static private LODLevelConfig GetConfigForLevel(LODElement element, LODLevel level) {
            switch (level) {
                case LODLevel.Close:
                    return element.Close;
                case LODLevel.Mid:
                    return element.Mid;
                case LODLevel.Far:
                    return element.Far;
                case LODLevel.SuperFar:
                default:
                    throw new ArgumentOutOfRangeException("level");
            }
        }
    }
}
