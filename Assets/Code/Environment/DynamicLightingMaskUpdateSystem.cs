using System;
using BeauUtil;
using FieldDay;
using FieldDay.Components;
using FieldDay.Systems;
using UnityEngine;

namespace Pennycook {
    [SysUpdate(GameLoopPhase.ApplicationPreRender, 10000)]
    public sealed class DynamicLightingMaskUpdateSystem : ComponentSystemBehaviour<DynamicLightingMask> {
        public override void ProcessWork(float deltaTime) {
            var lightingRegion = Find.State<InteriorLightingReference>();

            for(int i = 0; i < m_Components.Count; i++) {
                if (!Frame.Interval(4, i)) {
                    continue;
                }

                DynamicLightingMask mask = m_Components[i];
                Vector3 currentPos = mask.CachedTransform.position;
                if (currentPos == mask.LastTransformPosition) {
                    continue;
                }

                mask.LastTransformPosition = currentPos;
                Vector3 closestPos = lightingRegion.Region.ClosestPoint(currentPos);
                float distSq = Vector3.SqrMagnitude(currentPos - closestPos);
                bool isInInterior = distSq <= mask.Radius * mask.Radius;

                uint renderMask = isInInterior ? RenderingLayers.Interior_Mask : RenderingLayers.Default_Mask;

                foreach(var rend in mask.Renderers) {
                    rend.renderingLayerMask = renderMask;
                }
            }
        }

        protected override void OnComponentAdded(DynamicLightingMask component) {
            component.CacheComponent(ref component.CachedTransform);
            component.LastTransformPosition = default;
        }
    }
}