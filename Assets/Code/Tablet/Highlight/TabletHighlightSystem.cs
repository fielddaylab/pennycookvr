using System.Collections;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Systems;
using UnityEngine;

namespace Pennycook.Tablet {
    [SysUpdate(GameLoopPhaseMask.LateFixedUpdate | GameLoopPhaseMask.LateUpdate, 10)]
    public class TabletHighlightSystem : SharedStateSystemBehaviour<TabletHighlightState, TabletToolState, TabletControlState> {
        public override void ProcessWork(float deltaTime) {
			bool isGripping = !m_StateC.GrippedHandMask.IsEmpty;
            LayerMask searchMask = m_StateB.CurrentToolDef.RaycastMask;
            bool allowVisualHighlights = (m_StateB.CurrentToolDef.Flags & TabletToolFlags.DoNotUseHighlightBox) == 0;

            if (!ReferenceEquals(m_StateA.HighlightedObject, null)) {
                if (!m_StateA.HighlightedObject || !m_StateA.HighlightedObject.isActiveAndEnabled || !isGripping || m_StateB.CurrentTool == TabletTool.None || searchMask == 0) {
                    ClearSelection(m_StateA, m_StateB, m_StateC);
                    return;
                }
            }

            if (GameLoop.IsPhase(GameLoopPhase.LateFixedUpdate)) {
                if (Frame.Interval(3) && isGripping && m_StateB.CurrentTool != TabletTool.None && !m_StateA.RaycastJob.IsValid()) {
                    if (searchMask != 0) {
                        TabletZoomState zoomState = Find.State<TabletZoomState>();
                        float coneDistance = 25 * zoomState.ZoomMultiplier;
                        float coneRadius = coneDistance * m_StateB.CurrentToolDef.RaycastUnitConeRadius * CameraHelper.UnitHeightForFOV(m_StateA.LookCamera.fieldOfView) / 2;

                        Log.Trace("cone radius = {0}", coneRadius);

                        m_StateA.CachedLookCameraTransform.GetPositionAndRotation(out Vector3 cameraPos, out Quaternion cameraRot);
                        m_StateA.RaycastJob = RaycastJobs.SmoothConeCast(cameraPos, Geom.Forward(cameraRot), coneRadius, coneDistance, Mathf.CeilToInt(coneRadius / 2), searchMask);
                        RaycastJobs.Kick(ref m_StateA.RaycastJob);
                    }
                }
                return;
            }

            if (m_StateA.RaycastJob.IsValid()) {
                TabletHighlightable scannable;
                RaycastHit hit;
                scannable = RaycastJobs.Analyze(ref m_StateA.RaycastJob, m_StateB.CurrentToolDef.HighlightPredicate, m_StateA, out hit);

                m_StateA.RaycastJob.Clear();

                if (!scannable) {
                    if (m_StateA.HighlightedObject != null) {
                        ClearSelection(m_StateA, m_StateB, m_StateC);
                    }
                } else {
                    Rect viewportRect = TabletUtility.CalculateViewportAlignedClampedBoundingBox(scannable.HighlightCollider.bounds, m_StateA.LookCamera, m_StateA.CachedHighlightCornerScale);

                    if (m_StateA.HighlightedObject != scannable) {
                        SetSelection(m_StateA, m_StateB, m_StateC, scannable, viewportRect, allowVisualHighlights);

                        if ((m_StateB.CurrentToolDef.Flags & TabletToolFlags.SkipHapticHighlightFeedback) == 0) {
                            float vibAmp = Mathf.Clamp(1 - hit.distance / 60, 0.4f, 1) * 0.3f;
                            TabletUtility.PlayHaptics(vibAmp, 0.02f);
                        }
                    } else {
                        m_StateA.TargetHighlightCorners = viewportRect;
                    }
                }
            }

            if (m_StateA.IsBoxVisible && m_StateA.HighlightedObject) {
                if (!allowVisualHighlights) {
                    m_StateA.BoxTransitionRoutine.Replace(m_StateA, ScaleBoxDown(m_StateA));
                } else {
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
        }

        static private void SetSelection(TabletHighlightState highlight, TabletToolState toolState, TabletControlState ctrl, TabletHighlightable scannable, Rect rect, bool visualHighlight = true) {
            bool wasNotSelected = !highlight.HighlightedObject;

            if (!wasNotSelected) {
                VRGame.Events.Queue(GameEvents.ObjectUnhighlighted, EvtArgs.Ref(highlight.HighlightedObject));
                toolState.CurrentToolDef.OnUnhighlighted?.Invoke(highlight.HighlightedObject, ctrl);
            }

            VRGame.Events.Queue(GameEvents.ObjectHighlighted, EvtArgs.Ref(scannable));

            highlight.HighlightedObject = scannable;
            highlight.TargetHighlightCorners = rect;

            if (visualHighlight) {
                if (!highlight.IsBoxVisible) {
                    highlight.HighlightBox.sizeDelta = default;
                    highlight.HighlightBox.anchoredPosition = rect.center;
                    highlight.BoxTransitionRoutine.Replace(highlight, FadeBoxIn(highlight));
                } else if (wasNotSelected) {
                    highlight.BoxTransitionRoutine.Replace(highlight, FadeBoxIn(highlight));
                }
            } else if (highlight.IsBoxVisible) {
                highlight.BoxTransitionRoutine.Replace(highlight, ScaleBoxDown(highlight));
            }

            toolState.CurrentToolDef.OnHighlighted?.Invoke(highlight.HighlightedObject, ctrl);
        }

        static private void ClearSelection(TabletHighlightState highlight, TabletToolState toolState, TabletControlState ctrl) {
            VRGame.Events.Queue(GameEvents.ObjectUnhighlighted, EvtArgs.Ref(highlight.HighlightedObject));
            toolState.CurrentToolDef.OnUnhighlighted?.Invoke(highlight.HighlightedObject, ctrl);
            highlight.HighlightedObject = null;
            highlight.TargetHighlightCorners = highlight.HighlightBox.rect;
            if (highlight.IsBoxVisible) {
                highlight.BoxTransitionRoutine.Replace(highlight, ScaleBoxDown(highlight));
            }
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