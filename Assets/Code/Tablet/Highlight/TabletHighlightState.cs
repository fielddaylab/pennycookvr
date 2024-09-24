using System;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.Scripting;
using FieldDay.SharedState;
using Leaf.Runtime;
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

        [Header("Raycast Configuration")]
        public float RaycastSize = 0.4f;
        public float RaycastMinDistance = 1;

        [NonSerialized] public Transform CachedLookCameraTransform;
        [NonSerialized] public Vector2 CachedHighlightCornerScale;

        [NonSerialized] public TabletHighlightable HighlightedObject;
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

    static public partial class TabletUtility {
        static private readonly RaycastHit[] s_RaycastHitBuffer = new RaycastHit[32];

        // iteration count for spherecast
        private const int IterationCount = 8;

        // uncomment the default and solid masks to allow for other objects to occlude the target type
        public const int DefaultSearchMask = /* LayerMasks.Default_Mask | LayerMasks.Solid_Mask | */ LayerMasks.Grabbable_Mask | LayerMasks.Highlightable_Mask;
        public const int TravelSearchMask = /* LayerMasks.Default_Mask | LayerMasks.Solid_Mask | */ LayerMasks.Warpable_Mask;

        static public TabletHighlightable FindBestHighlightableAlongRay(Ray ray, LayerMask mask, float raySize, float minDistance, float maxDistance, out float outDist) {
            // iterative
            float distanceSeg = maxDistance / IterationCount;
            for(int i = 0; i < IterationCount; i++) {
                float size = raySize * (distanceSeg * (i + 0.5f) / minDistance);
                float distance = distanceSeg + Math.Sign(i) * size;
                Ray r = new Ray(ray.GetPoint(distanceSeg * i - Math.Sign(i) * size), ray.direction);
                if (Physics.SphereCast(r, size, out RaycastHit hit, distance, mask)) {
                    //DebugDraw.AddLine(r.origin, hit.point, Color.red.WithAlpha(0.2f), size * 2f, 0.1f, false);
                    TabletHighlightable highlightable = hit.collider.GetComponent<TabletHighlightable>();
                    Rigidbody body;
                    if (!highlightable && (body = hit.rigidbody)) {
                        highlightable = body.GetComponent<TabletHighlightable>();
                    } else {
                        highlightable = hit.collider.GetComponentInParent<TabletHighlightable>();
                    }
                    if (highlightable) {
                        outDist = distance + hit.distance;
                        return highlightable;
                    }
                } else {
                    //DebugDraw.AddLine(r.origin, r.GetPoint(distanceSeg), Color.blue.WithAlpha(0.2f), size * 2f, 0.1f, false);
                }
            }
            outDist = -1;
            return null;
        }

        static private TabletHighlightable FindBestHighlightableAlongRay_ScoredMethod(Ray ray, LayerMask mask, float maxDistance) {
            int allCasted = Physics.SphereCastNonAlloc(ray, 1.25f, s_RaycastHitBuffer, maxDistance, mask);
            if (allCasted == 0) {
                return null;
            }

            RaycastHit hit = default;
            float hitAlignment = 0;
            for (int i = 0; i < allCasted; i++) {
                RaycastHit potential = s_RaycastHitBuffer[i];
                Vector3 potentialPos;
                Rigidbody body;
                if ((body = potential.rigidbody)) {
                    potentialPos = body.position;
                } else {
                    potentialPos = potential.collider.bounds.center;
                }

                float alignment = Vector3.Dot(ray.direction, potentialPos - ray.origin);
                if (alignment < 0.7f) {
                    continue;
                }

                if (alignment > hitAlignment) {
                    hit = potential;
                    hitAlignment = alignment;
                }
            }

            Array.Clear(s_RaycastHitBuffer, 0, allCasted);

            if (hit.colliderInstanceID != 0) {
                //DebugDraw.AddLine(ray.origin, hit.point, Color.blue, 0.2f, 0.1f);
                TabletHighlightable highlightable = hit.collider.GetComponent<TabletHighlightable>();
                Rigidbody body;
                if (!highlightable && (body = hit.rigidbody)) {
                    highlightable = body.GetComponent<TabletHighlightable>();
                } else {
                    highlightable = hit.collider.GetComponentInParent<TabletHighlightable>();
                }
                return highlightable;
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

			Vector2* viewCorners = stackalloc Vector2[8];
            for (int i = 0; i < 8; i++) {
                viewCorners[i] = ClampTo01Space(referenceCamera.WorldToViewportPoint(corners[i], Camera.MonoOrStereoscopicEye.Mono));
            }

            Rect r = Geom.MinRect(new UnsafeSpan<Vector2>(viewCorners, 8));
            r.x *= scale.x;
            r.y *= scale.y;
            r.width *= scale.x;
            r.height *= scale.y;
            return r;
        }
		
		static private Vector2 ClampTo01Space(Vector3 input) {
			Vector2 output;
			output.x = Mathf.Clamp01(input.x);
			output.y = Mathf.Clamp01(input.y);
			return output;
		}

        [LeafMember("IsTabletHighlighted")]
        static private bool LeafIsHighlighted(ScriptActor actor) {
            if (actor == null) {
                return false;
            }

            if (!actor.TryGetComponent(out TabletHighlightable h)) {
                Log.Warn("IsTabletHighlighted(): Actor '{0}' is not highlightable", actor);
                return false;
            }
            
            return Find.State<TabletHighlightState>().HighlightedObject == h;
        }
    }
}