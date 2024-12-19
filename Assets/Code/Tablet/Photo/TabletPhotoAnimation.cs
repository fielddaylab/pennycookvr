using System;
using System.Collections;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.UI;

namespace Pennycook.Tablet {
    public sealed class TabletPhotoAnimation : MonoBehaviour {
        public enum State {
            Ready,
            Playing,
            Finished
        }

        public LayoutOffset Offset;
        public CanvasGroup Group;
        public RectTransform PhotoTransform;
        public RawImage PhotoRenderer;

        public Material DefaultMaterial;
        public Material FailureMaterial;

        public ParticleSystem SuccessParticles;

        [NonSerialized] public State CurrentState;
        [NonSerialized] public Routine Animation;

        private void Awake() {
            Group.gameObject.SetActive(false);
        }

        public void PlayPhotoAnimation(TabletPhoto photo) {
            Assert.True(CurrentState == State.Ready);
            CurrentState = State.Playing;
            PhotoRenderer.texture = photo.Texture;
            PhotoTransform.SetRotation(RNG.Instance.NextFloat(-5, -8), Axis.Z, Space.Self);

            Animation.Replace(this, AnimationSequence(photo.Result)).ExecuteWhileDisabled();
        }

        private IEnumerator AnimationSequence(TabletPhotoResult result) {
            Group.gameObject.SetActive(true);
            Offset.Offset0 = new Vector2(0, -400);

            yield return null;
            yield return Offset.Offset0To(default, 0.3f).Ease(Curve.BackOut);

            if (result == TabletPhotoResult.NewBehavior) {
                SuccessParticles.Play();
                yield return 0.8f;
            } else if (result == TabletPhotoResult.BadPhoto) {
                yield return Offset.Offset1To(new Vector2(8, 0), 0.3f).Wave(Wave.Function.Cos, 4);
                yield return 0.1f;
            }

            yield return Offset.Offset0To(new Vector2(0, -400), 0.3f).Ease(Curve.QuadIn);

            Group.gameObject.SetActive(false);

            CurrentState = State.Finished;
        }
    }
}