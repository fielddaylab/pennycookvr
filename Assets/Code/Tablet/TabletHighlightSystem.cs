using System.Collections;
using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using FieldDay.Systems;
using UnityEngine;

namespace Pennycook.Tablet {
    [SysUpdate(GameLoopPhase.Update, 10)]
    public class TabletHighlightSystem : SharedStateSystemBehaviour<TabletHighlightState, TabletToolState, TabletControlState> {
        public override void ProcessWork(float deltaTime) {
			bool isGripping = !m_StateC.GrippedHandMask.IsEmpty;
			
            if (!ReferenceEquals(m_StateA.HighlightedObject, null)) {
                if (!m_StateA.HighlightedObject || !m_StateA.HighlightedObject.isActiveAndEnabled || !isGripping) {
                    ClearSelection(m_StateA);
                    return;
                }
            }

            if (Frame.Interval(5) && isGripping) {
				TabletZoomState zoomState = Find.State<TabletZoomState>();
				
                m_StateA.CachedLookCameraTransform.GetPositionAndRotation(out Vector3 cameraPos, out Quaternion cameraRot);
                Ray r = new Ray(cameraPos, Geom.Forward(cameraRot));
                TabletHighlightable scannable = TabletUtility.FindBestHighlightableAlongRay(r, 40 * zoomState.ZoomMultiplier);

                if (!scannable) {
                    if (m_StateA.HighlightedObject != null) {
                        ClearSelection(m_StateA);
                    }
                } else {
                    Rect viewportRect = TabletUtility.CalculateViewportAlignedBoundingBox(scannable.HighlightCollider.bounds, m_StateA.LookCamera, m_StateA.CachedHighlightCornerScale);

                    if (m_StateA.HighlightedObject != scannable) {
                        SetSelection(m_StateA, scannable, viewportRect);
                    } else {
                        m_StateA.TargetHighlightCorners = viewportRect;
                    }
                }
            }

            if (m_StateA.HighlightedObject) {
                Vector2 targetAnchor = m_StateA.TargetHighlightCorners.center;
                Vector2 targetSize = m_StateA.TargetHighlightCorners.size;

                Vector2 anchor = m_StateA.HighlightBox.anchoredPosition;
                Vector2 size = m_StateA.HighlightBox.sizeDelta;

                float lerpAmt = TweenUtil.Lerp(2, deltaTime);

                if (!Mathf.Approximately(anchor.x, targetAnchor.x) || !Mathf.Approximately(anchor.y, targetAnchor.y)) {
                    anchor = Vector2.Lerp(anchor, targetAnchor, lerpAmt);
                    m_StateA.HighlightBox.anchoredPosition = anchor;
                }

                if (!Mathf.Approximately(size.x, targetSize.x) || !Mathf.Approximately(size.y, targetSize.y)) {
                    size = Vector2.Lerp(size, targetSize, lerpAmt);
                    m_StateA.HighlightBox.sizeDelta = size;
                }
            }
        }

        static private void SetSelection(TabletHighlightState highlight, TabletHighlightable scannable, Rect rect) {
            bool wasNotSelected = !highlight.HighlightedObject;

            if (!wasNotSelected) {
                VRGame.Events.Queue(GameEvents.ObjectUnhighlighted, EvtArgs.Ref(highlight.HighlightedObject));
            }

            VRGame.Events.Queue(GameEvents.ObjectHighlighted, EvtArgs.Ref(scannable));

            highlight.HighlightedObject = scannable;
            highlight.TargetHighlightCorners = rect;
            if (!highlight.IsBoxVisible) {
                highlight.HighlightBox.sizeDelta = default;
                highlight.HighlightBox.anchoredPosition = rect.center;
                highlight.BoxTransitionRoutine.Replace(highlight, FadeBoxIn(highlight));
            } else if (wasNotSelected) {
                highlight.BoxTransitionRoutine.Replace(highlight, FadeBoxIn(highlight));
            }

            highlight.HighlightShortLabelGroup.gameObject.SetActive(true);
            highlight.HighlightShortLabel.SetText(scannable.ShortLabel);

            highlight.DetailsHeader.gameObject.SetActive(true);
            highlight.DetailsHeader.SetText(scannable.DetailedHeader);

            highlight.DetailsText.gameObject.SetActive(true);
            highlight.DetailsText.SetText(scannable.DetailedText);
        }

        static private void ClearSelection(TabletHighlightState highlight) {
            ClearObjectDetails(highlight);
            VRGame.Events.Queue(GameEvents.ObjectUnhighlighted, EvtArgs.Ref(highlight.HighlightedObject));
            highlight.HighlightedObject = null;
            highlight.TargetHighlightCorners = highlight.HighlightBox.rect;
            if (highlight.IsBoxVisible) {
                highlight.BoxTransitionRoutine.Replace(highlight, ScaleBoxDown(highlight));
            }
        }

        static private void ClearObjectDetails(TabletHighlightState highlight) {
            highlight.DetailsText.gameObject.SetActive(false);
            highlight.DetailsHeader.gameObject.SetActive(false);

            highlight.HighlightShortLabelGroup.gameObject.SetActive(false);
        }

        static private IEnumerator ScaleBoxDown(TabletHighlightState highlight) {
            yield return Routine.Combine(
                highlight.HighlightBox.SizeDeltaTo(0, 0.2f, Axis.XY).Ease(Curve.CubeOut),
                highlight.HighlightBoxGroup.FadeTo(0, 0.1f)
            );
            highlight.HighlightBox.gameObject.SetActive(false);
            highlight.IsBoxVisible = false;
        }

        static private IEnumerator FadeBoxIn(TabletHighlightState highlight) {
            highlight.IsBoxVisible = true;
            highlight.HighlightBox.gameObject.SetActive(true);
            yield return highlight.HighlightBoxGroup.FadeTo(1, (1 - highlight.HighlightBoxGroup.alpha) * 0.05f);
        }
    }
}