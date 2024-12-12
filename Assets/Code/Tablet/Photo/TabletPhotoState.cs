using System;
using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.SharedState;
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

        [NonSerialized] public Stage CurrentStage;
        [NonSerialized] public double NextAllowedPhotoTS;
        [NonSerialized] public float Cooldown;

        [NonSerialized] public FixedPool<TabletPhoto> PhotoPool;
        [NonSerialized] public RingBuffer<TabletPhoto> ActivePhotos;

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
                return new TabletPhoto() {
                    Texture = tex
                };
            });
            PhotoPool.Prewarm();

            ActivePhotos = new RingBuffer<TabletPhoto>(PhotoPool.Capacity);
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
        static public void TakePhoto(double ts) {
            TabletPhotoState photoState = Find.State<TabletPhotoState>();
            RequestPhoto(photoState);
            photoState.NextAllowedPhotoTS = ts + 2;
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
    }
}