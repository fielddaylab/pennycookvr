using System;
using System.Collections;
using System.IO;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.SharedState;
using FieldDay.Systems;
using UnityEngine;
using UnityEngine.Rendering;

namespace Pennycook.Tablet {
    [SysUpdate(GameLoopPhaseMask.PreUpdate | GameLoopPhaseMask.UnscaledLateUpdate, 10000)]
    public sealed class TabletPhotoSystem : SharedStateSystemBehaviour<TabletPhotoState> {
        public override void ProcessWork(float deltaTime) {
            if (GameLoop.IsPhase(GameLoopPhase.PreUpdate)) {
                if (m_State.CurrentStage == TabletPhotoState.Stage.PhotoReady) {
                    TabletPhoto photo = m_State.QueuedPhoto;
                    PhotoUtility.CopyRTToTextureCentered(m_State.PhotoRT, photo.Texture);
                    photo.Texture.Apply();
                    UploadPhoto(photo, DateTime.Now);
                    m_State.ActivePhotos.PushBack(photo);
                    m_State.Flash.gameObject.SetActive(true);
                    m_State.Flash.alpha = 1;
                    m_State.CurrentStage = TabletPhotoState.Stage.Cooldown;
                    m_State.Cooldown = 1f;
                    m_State.QueuedPhoto = null;
                }
                return;
            }

            switch (m_State.Animation.CurrentState) {
                case TabletPhotoAnimation.State.Ready: {
                    if (m_State.ActivePhotos.TryPeekFront(out TabletPhoto photo)) {
                        m_State.Animation.PlayPhotoAnimation(photo);
                    }
                    break;
                }

                case TabletPhotoAnimation.State.Finished: {
                    if (m_State.ActivePhotos.TryPopFront(out TabletPhoto photo)) {
                        m_State.PhotosPendingCleanup.PushBack(photo);
                    }
                    m_State.Animation.CurrentState = TabletPhotoAnimation.State.Ready;
                    break;
                }
            }

            for(int i = m_State.PhotosPendingCleanup.Count - 1; i >= 0; i--) {
                TabletPhoto pendingPhoto = m_State.PhotosPendingCleanup[i];
                if (!pendingPhoto.UploadHandle.IsRunning()) {
                    m_State.PhotosPendingCleanup.FastRemoveAt(i);
                    m_State.PhotoPool.Free(pendingPhoto);
                }
            }

            switch (m_State.CurrentStage) {
                case TabletPhotoState.Stage.Cooldown: {
                    m_State.Cooldown -= deltaTime;
                    float percent = Math.Max(0, m_State.Cooldown / 1f);
                    if (percent <= 0) {
                        m_State.Cooldown = 0;
                        m_State.CurrentStage = TabletPhotoState.Stage.Idle;
                        m_State.Flash.gameObject.SetActive(false);
                    } else {
                        m_State.Flash.alpha = percent;
                    }
                    break;
                }
            }
        }

        static private void UploadPhoto(TabletPhoto photo, DateTime timestamp) {
            photo.Timestamp = timestamp;
            photo.CachedSize = new Vector2Int(photo.Texture.width, photo.Texture.height);
            photo.CachedFormat = photo.Texture.graphicsFormat;
            photo.CachedCPUData = photo.Texture.GetRawTextureData<byte>();
            
            photo.UploadHandle = Async.Schedule(EncodeAndUpload(photo), AsyncFlags.HighPriority | AsyncFlags.MainThreadOnly);
        }

        static private IEnumerator EncodeAndUpload(TabletPhoto photo) {
            var nativeBytes = ImageConversion.EncodeNativeArrayToJPG(photo.CachedCPUData, photo.CachedFormat, (uint) photo.CachedSize.x, (uint) photo.CachedSize.y);
            yield return null;
            byte[] nativeByteArr = nativeBytes.ToArray();
            nativeBytes.Dispose();
            yield return null;
            string directory;
#if UNITY_EDITOR
            directory = "DebugPhotos/";
#else
            directory = Path.Combine(Application.persistentDataPath, "Photos/");
#endif // UNITY_EDITOR

            Directory.CreateDirectory(directory);
            yield return null;

            string fileName = string.Format("Photo_{0}_{1}.jpg", photo.Tag, photo.Timestamp.ToString("dd-MM-yyyy-HHmmss"));
            File.WriteAllBytes(Path.Combine(directory, fileName), nativeByteArr);

            Log.Msg("[TabletPhotoSystem] Uploaded photo '{0}'", fileName);
        }
    }
}