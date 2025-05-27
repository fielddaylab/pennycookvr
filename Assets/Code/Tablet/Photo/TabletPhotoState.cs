using System;
using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Scripting;
using FieldDay.SharedState;
using Leaf.Runtime;
using UnityEngine;
using UnityEngine.Rendering;

namespace Pennycook.Tablet {
    public sealed class TabletPhotoState : SharedStateComponent, IRegistrationCallbacks, ICameraPostRenderCallback {
        [Header("Cameras")]
        public Camera PhotoCamera;
        public FixedCameraRefreshRate PhotoCameraRefresher;
        public Camera UICamera;

        [Header("Render Targets")]
        public RenderTexture DefaultRT;
        public RenderTexture PhotoRT;

        [Header("Misc")]
        public Canvas[] UICanvases;
        public Material TabletMaterial;
        public CanvasGroup Flash;
        public TabletPhotoAnimation Animation;

        [NonSerialized] public Stage CurrentStage;
        [NonSerialized] public double NextAllowedPhotoTS;
        [NonSerialized] public float Cooldown;

        [NonSerialized] public TabletPhoto QueuedPhoto;
        [NonSerialized] public FixedPool<TabletPhoto> PhotoPool;
        [NonSerialized] public RingBuffer<TabletPhoto> ActivePhotos;
        [NonSerialized] public RingBuffer<TabletPhoto> PhotosPendingCleanup;

        public enum Stage {
            Idle,
            PhotoRequested,
            PhotoReady,
            Cooldown,
        }

        void ICameraPostRenderCallback.OnCameraPostRender(Camera inCamera, CameraCallbackSource inSource) {
            if (CurrentStage == Stage.PhotoRequested) {
                Log.Msg("[TabletPhotoState] Photo has been rendered");
                PhotoUtility.RestoreCameras(this);
            }
        }

        void IRegistrationCallbacks.OnRegister() {
            CameraHelper.AddOnPostRender(PhotoCamera, this);

            PhotoPool = new FixedPool<TabletPhoto>(4, (p) => {
                Texture2D tex = new Texture2D(PhotoRT.width, PhotoRT.height, TextureFormat.RGB24, false);
                tex.name = "TabletPhoto";
                tex.filterMode = FilterMode.Bilinear;
                return new TabletPhoto() {
                    Texture = tex
                };
            });
            PhotoPool.Prewarm();

            ActivePhotos = new RingBuffer<TabletPhoto>(PhotoPool.Capacity);
            PhotosPendingCleanup = new RingBuffer<TabletPhoto>(PhotoPool.Capacity);
        }

        void IRegistrationCallbacks.OnDeregister() {
            CameraHelper.RemoveOnPostRender(PhotoCamera, this);

            PhotoPool.Dispose();
            PhotoPool = null;

            ActivePhotos.Clear();
            ActivePhotos = null;
        }
    }

    static public partial class PhotoUtility {
        static public void TakePhoto(TabletHighlightable highlighted, double ts) {
            TabletPhotoState photoState = Find.State<TabletPhotoState>();
            RequestPhoto(photoState);
            photoState.NextAllowedPhotoTS = ts + 2;
            photoState.QueuedPhoto = photoState.PhotoPool.Alloc();
            photoState.QueuedPhoto.Tag = GetPhotoTag(highlighted);
            HandleBehaviorCapture(highlighted);
            TabletUtility.PlayHaptics(0.3f, 0.08f);
            TabletUtility.PlaySfx("Tablet.Photo.Snap");
        }

        static public void RequestPhoto(TabletPhotoState photoState) {
            Assert.True(photoState.CurrentStage == TabletPhotoState.Stage.Idle);
            
            photoState.PhotoCamera.targetTexture = photoState.PhotoRT;
            photoState.PhotoCameraRefresher.Passthrough = true;
            photoState.UICamera.cullingMask = 0;

            foreach(var canvas in photoState.UICanvases) {
                canvas.enabled = false;
            }
            
            photoState.CurrentStage = TabletPhotoState.Stage.PhotoRequested;
        }

        static public void RestoreCameras(TabletPhotoState photoState) {
            Assert.True(photoState.CurrentStage == TabletPhotoState.Stage.PhotoRequested);

            photoState.PhotoCamera.targetTexture = photoState.DefaultRT;
            photoState.PhotoCameraRefresher.Passthrough = false;
            photoState.PhotoCameraRefresher.TimeBeforeNextRefresh = 0;
            photoState.UICamera.cullingMask = LayerMasks.OffscreenRendering_Mask;

            foreach (var canvas in photoState.UICanvases) {
                canvas.enabled = true;
            }

            photoState.CurrentStage = TabletPhotoState.Stage.PhotoReady;
        }

        static public string GetPhotoTag(TabletHighlightable highlightable) {
            if (!highlightable) {
                return null;
            }
            if (highlightable.CachedCapture && !highlightable.CachedCapture.CaptureId.IsEmpty) {
                return highlightable.CachedCapture.CaptureId.Source();
            }
            if (!string.IsNullOrEmpty(highlightable.PhotoTag)) {
                return highlightable.PhotoTag;
            }
            ScriptActor actor = ScriptUtility.Actor(highlightable);
            if (actor) {
                return actor.gameObject.name;
            }

            return highlightable.gameObject.name;
        }

        static private Rect CalculatePhotoSubjectRect(TabletHighlightState highlightState, TabletHighlightable highlightable) {
            return TabletUtility.CalculateViewportAlignedBoundingBox(highlightable.HighlightCollider.bounds, highlightState.LookCamera, Vector2.one);
        }

        static private bool DetermineGoodFraming(Rect rect, TabletHighlightState highlightState, TabletCapturable capturable) {
            if (rect.width < 0.1f || rect.height < 0.1f) {
                Log.Msg("Subject width/height too small");
                return false;
            }

            if ((rect.width * rect.height) < (0.125f * 0.125f)) {
                Log.Msg("Subject area too small");
                return false;
            }

            if (capturable && capturable.CanCapture) {
                float threshold = capturable.ViewAlignmentThreshold * 2 - 1;
                float dot = Vector3.Dot(highlightState.CachedLookCameraTransform.forward, capturable.transform.forward);
                if (dot < threshold) {
                    Log.Msg("Subject not facing camera");
                    return false;
                }
            }

            Log.Msg("Subject is framed well");
            return true;
        }

        static private TabletPhotoResult HandleBehaviorCapture(TabletHighlightable highlightable) {
            if (!highlightable) {
                return TabletPhotoResult.Nothing;
            }
            
            TabletHighlightState highlightState = Find.State<TabletHighlightState>();
            TabletGoalState goals = Find.State<TabletGoalState>();

            TabletCapturable cap = highlightable.CachedCapture;
            Rect framing = CalculatePhotoSubjectRect(highlightState, highlightable);

            bool isBadFraming = !DetermineGoodFraming(framing, highlightState, cap);

            bool newGlobalBehavior, newUniqueBehavior, wasPerformingBehavior;

            if (!isBadFraming && cap && cap.CanCapture && !cap.CaptureId.IsEmpty && goals.RelevantCaptureIds.Contains(cap.CaptureId)) {
                TabletInventory inv = Find.State<TabletInventory>();
                CaptureRecord rec = new CaptureRecord(cap, cap.CaptureId);
                newGlobalBehavior = inv.GlobalCapturedBehaviors.Add(cap.CaptureId);
                newUniqueBehavior = inv.CaptureRecords.Add(rec);
                wasPerformingBehavior = true;
            } else {
                newGlobalBehavior = newUniqueBehavior = wasPerformingBehavior = false;
            }

            LeafThreadHandle thread = default;

            using (var t = TempVarTable.Alloc()) {
                ScriptActor a = ScriptUtility.Actor(highlightable);
                t.ActorInfo(a);
                t.Set("behaviorId", cap ? cap.CaptureId : StringHash32.Null);
                t.Set("isBadFraming", isBadFraming);
                t.Set("wasPerformingBehavior", wasPerformingBehavior);

                if (newGlobalBehavior) {
                    thread = ScriptUtility.Trigger(GameTriggers.TabletNewBehaviorCaptured, t);
                }

                if (newUniqueBehavior && !thread.IsRunning()) {
                    thread = ScriptUtility.Trigger(GameTriggers.TabletNewBehaviorInstanceCaptured, t);
                } else {
                    ScriptUtility.Invoke(GameTriggers.TabletNewBehaviorInstanceCaptured, t);
                }

                if(wasPerformingBehavior) {
                    PlayerProgressState state = Find.State<PlayerProgressState>();
                    if(state.DayIndex == 1) {
                        VRGame.Events.Dispatch(GameEvents.PhotoBehavior, EvtArgs.Create(new Data.BehaviorCaptureInfo(a.Id, Data.BehaviorType.MATING_DANCE)));
                    } else if (state.DayIndex == 2) {
                        VRGame.Events.Dispatch(GameEvents.PhotoBehavior, EvtArgs.Create(new Data.BehaviorCaptureInfo(a.Id, Data.BehaviorType.REGURGITATION)));
                    }
                }

                if (!thread.IsRunning()) {
                    thread = ScriptUtility.Trigger(GameTriggers.TabletPhotoTaken, t);
                } else {
                    ScriptUtility.Invoke(GameTriggers.TabletPhotoTaken, t);
                }
            }

            if (newGlobalBehavior) {
                return TabletPhotoResult.NewBehavior;
            } else if (isBadFraming && wasPerformingBehavior) {
                return TabletPhotoResult.BadPhoto;
            } else {
                return TabletPhotoResult.GoodPhoto;
            }
        }
    }
}