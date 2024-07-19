using BeauUtil;
using FieldDay;
using FieldDay.Systems;
using UnityEngine;

namespace Pennycook.Tablet {
    [SysUpdate(GameLoopPhase.LateUpdate)]
    public class TabletCameraCullingSystem : SharedStateSystemBehaviour<TabletRenderState, PlayerRig> {
        public override void ProcessWork(float deltaTime) {
            if (!Frame.Interval(3)) {
                return;
            }

            Vector3 lookVecNorm = m_StateB.HeadLook.forward;
            Vector3 camVecNorm = m_StateA.ScreenCameraTransform.forward;

            Vector3 towardsVec = m_StateA.TabletRoot.position - m_StateB.HeadLook.position;
            Vector3 towardsVecNorm = towardsVec.normalized;

            float lookAtTablet = Vector3.Dot(lookVecNorm, towardsVecNorm);
            float facingScreen = Vector3.Dot(lookVecNorm, camVecNorm);

            bool canBeFacing = facingScreen > 0 && lookAtTablet > 0;
            if (Ref.Replace(ref m_StateA.CurrentlyFacingScreen, canBeFacing)) {
                m_StateA.ScreenCamera.enabled = canBeFacing;
                m_StateA.ScreenOverlayCamera.enabled = canBeFacing;
                m_StateA.ScreenOverlayCanvas.enabled = canBeFacing;
            }
        }
    }
}