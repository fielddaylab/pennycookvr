using System;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.SharedState;
using TMPro;
using UnityEngine;

namespace Pennycook.Tablet {
    public class TabletHighlightState : SharedStateComponent, IRegistrationCallbacks {
        public Camera LookCamera;

        [Header("Selection Box")]
        public RectTransform HighlightBox;
        public CanvasGroup HighlightBoxGroup;

        [Header("Selection Label")]
        public CanvasGroup HighlightShortLabelGroup;
        public TMP_Text HighlightShortLabel;

        [Header("Details")]
        public TMP_Text DetailsHeader;
        public TMP_Text DetailsText;

        [NonSerialized] public Transform CachedLookCameraTransform;
        [NonSerialized] public Vector2 CachedHighlightCornerScale;

        [NonSerialized] public TabletScannable HighlightedScannable;
        [NonSerialized] public Rect TargetHighlightCorners;
        [NonSerialized] public Routine BoxTransitionRoutine;
        [NonSerialized] public bool IsBoxVisible;

        void IRegistrationCallbacks.OnDeregister() {
        }

        void IRegistrationCallbacks.OnRegister() {
            LookCamera.CacheComponent(ref CachedLookCameraTransform);
            CachedHighlightCornerScale = ((RectTransform) HighlightBox.parent).rect.size;

            Log.Msg("[TabletHighlightState] Parent size is {0}", CachedHighlightCornerScale);
        }
    }

    static public class TabletHighlightUtility {
        //static private readonly RaycastHit[] s_RaycastHitBuffer = new RaycastHit[32];

        static public TabletScannable FindBestScannableAlongRay(Ray ray, float maxDistance) {
            const int layerMask = LayerMasks.Default_Mask | LayerMasks.Grabbable_Mask | LayerMasks.Scannable_Mask;
            // TODO: evaluate if we need a spherecast instead
            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, layerMask)) {
                //DebugDraw.AddLine(ray.origin, hit.point, Color.blue, 0.2f, 0.1f);
                TabletScannable scannable = hit.collider.GetComponent<TabletScannable>();
                Rigidbody body;
                if (!scannable && (body = hit.rigidbody)) {
                    scannable = body.GetComponent<TabletScannable>();
                }
                return scannable;
            } else {
                return null;
            }
        }

        static public unsafe Rect CalculateViewportAlignedBoundingBox(Bounds bounds, Camera referenceCamera, Vector2 scale) {
            Vector3* corners = stackalloc Vector3[8];
            Vector3 min = bounds.min, max = bounds.max;
            corners[0] = min;
            corners[1] = new Vector3(min.x, min.y, max.z);
            corners[2] = new Vector3(min.x, max.y, min.z);
            corners[3] = new Vector3(min.x, max.y, max.z);
            corners[4] = new Vector3(max.x, min.y, min.z);
            corners[5] = new Vector3(max.x, min.y, max.z);
            corners[6] = new Vector3(max.x, max.y, min.z);
            corners[7] = max;

            //DebugDraw.AddBounds(bounds, Color.blue.WithAlpha(0.2f), 0.2f, 0.1f);

            for (int i = 0; i < 8; i++) {
                corners[i] = referenceCamera.WorldToViewportPoint(corners[i], Camera.MonoOrStereoscopicEye.Mono);
            }

            Rect r = Geom.BoundsToRect(Geom.MinAABB(new UnsafeSpan<Vector3>(corners, 8)));
            r.x *= scale.x;
            r.y *= scale.y;
            r.width *= scale.x;
            r.height *= scale.y;
            return r;
        }
    }
}