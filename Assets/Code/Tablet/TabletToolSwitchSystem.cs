using FieldDay;
using FieldDay.Audio;
using FieldDay.HID;
using FieldDay.HID.XR;
using FieldDay.Systems;
using FieldDay.XR;

namespace Pennycook.Tablet {
    [SysUpdate(GameLoopPhase.Update, 1)]
    public class TabletToolSwitchSystem : SharedStateSystemBehaviour<TabletToolState, TabletControlState, XRInputState> {
        public override void ProcessWork(float deltaTime) {
            if (!m_StateA.AllowToolSwitch) {
                return;
            }

            int shift = 0;
            if (TabletUtility.ConsumeButtonPress(XRHandButtons.TriggerButton)) {
                shift = 1;
            }

            if (shift != 0) {
                int maxTools = m_StateA.Configs.Length;
                int newIdx = (m_StateA.CurrentToolIndex + maxTools + shift) % maxTools;
                TabletUtility.SetTool(m_StateA, newIdx, true);
            }
        }
    }
}