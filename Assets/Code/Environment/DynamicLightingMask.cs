using System;
using BeauUtil;
using FieldDay.Components;
using UnityEngine;

namespace Pennycook {
    public sealed class DynamicLightingMask : BatchedComponent {
        public Renderer[] Renderers;
        public float Radius = 1;

        [NonSerialized] public Transform CachedTransform;
        [NonSerialized] public Vector3 LastTransformPosition;

#if UNITY_EDITOR
        private void Reset() {
            Renderers = GetComponentsInChildren<Renderer>(true);
        }

        private void OnDrawGizmosSelected() {
            if (!Application.isPlaying) {
                Gizmos.color = ColorBank.Aqua.WithAlpha(0.5f);
                Gizmos.DrawSphere(transform.position, Radius);
            }
        }
#endif // UNITY_EDITOR
    }

    static public class DynamicLightingUtility {
        static public void SetLightingMask(DynamicLightingMask mask, uint layerMask) {
            foreach(var rend in mask.Renderers) {
                rend.renderingLayerMask = layerMask;
            }
        }
    }
}