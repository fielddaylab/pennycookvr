using FieldDay;
using FieldDay.Audio;
using FieldDay.Debugging;
using FieldDay.HID.XR;
using FieldDay.Systems;
using FieldDay.XR;

namespace Pennycook.Tablet {
    [SysUpdate(GameLoopPhase.Update)]
    public class TabletZoomSystem : SharedStateSystemBehaviour<TabletZoomState, TabletControlState, XRInputState, TabletRenderState> {
        public override void ProcessWork(float deltaTime) {
            if (DebugInput.IsPressed(UnityEngine.KeyCode.A)) {
                Sfx.OneShot("ZoomChanged", m_StateD.TabletRoot);
            }

            if (m_StateB.GrippedHandMask.IsSet((int) XRHandIndex.Right)) {

                if (m_StateC.RightHand.Buttons.ConsumePress(XRHandButtons.PrimaryAxisUp)) {
                    if (m_StateA.ZoomIndex < m_StateA.ZoomLevels.Length - 1) {
                        TabletUtility.AdjustZoom(m_StateA, m_StateA.ZoomIndex + 1, true);
                    } else {
                        // TODO: play sound
                    }
                } else if (m_StateC.RightHand.Buttons.ConsumePress(XRHandButtons.PrimaryAxisDown)) {
                    if (m_StateA.ZoomIndex > 0) {
                        TabletUtility.AdjustZoom(m_StateA, m_StateA.ZoomIndex - 1, true);
                    } else {
                        // TODO: play sound
                    }
                }
            }
        }
    }
}