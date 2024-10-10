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
            if (TabletUtility.ConsumeButtonPress(XRHandButtons.Secondary)) {
                TabletUtility.AdjustZoom(m_StateA, (m_StateA.ZoomIndex + 1) % m_StateA.ZoomLabels.Length, true);
            }
        }
    }
}