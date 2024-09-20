using System;
using BeauUtil;
using FieldDay.Components;
using FieldDay.XR;
using UnityEngine;

namespace FieldDay.VRHands {
    public class GrabPosedHand : BatchedComponent {
        #region Inspector

        [Required] public Grabber Grabber;
        public Animator Animator;

        [Header("Offsets")]
        public Quaternion Rotation = Quaternion.identity;

        #endregion // Inspector

        [NonSerialized] public Transform CachedTransform;
        [NonSerialized] public XRTrackedTransform CachedTracked;

        [NonSerialized] public bool WasGripPosed;

        [NonSerialized] public int AnimLayerIndexPoint = -1;
		[NonSerialized] public int AnimParamIndexFlex = -1;
		[NonSerialized] public int AnimParamIndexPose = -1;

        private void Awake() {
            this.CacheComponent(ref CachedTransform);
            this.CacheComponent(ref CachedTracked);
            
            if(Animator != null) {
				AnimLayerIndexPoint = Animator.GetLayerIndex("Point Layer");
                AnimParamIndexFlex = Animator.StringToHash("Flex");
			    AnimParamIndexPose = Animator.StringToHash("Pose");

                Animator.SetInteger(AnimParamIndexPose, 0);
                Animator.SetFloat("Pinch", 0f);
			}
        }

        public void AnimateGrip(float grip) {
            grip = Mathf.Clamp(grip, 0.0f, 0.5f);
            Animator.SetFloat(AnimParamIndexFlex, grip);
            Animator.SetLayerWeight(AnimLayerIndexPoint, 1.0f-grip*2f);
        }
    }
}