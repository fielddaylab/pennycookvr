using BeauUtil;
using FieldDay;
using FieldDay.Audio;
using FieldDay.Debugging;
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
            if (TabletUtility.ConsumeButtonPress(XRHandButtons.Primary) || DebugInput.IsPressed(UnityEngine.KeyCode.P)) {
                shift = 1;
            }

            if (shift != 0) {
                int maxTools = m_StateA.Configs.Length;
                if(m_StateA.NoCount) {
                    maxTools = maxTools - 1;
                }
                int newIdx = (m_StateA.CurrentToolIndex + maxTools + shift) % maxTools;
                VRGame.Events.Dispatch(GameEvents.MargoModeSwitch, EvtArgs.Create(newIdx));
                TabletUtility.SetTool(m_StateA, newIdx, true);
            }
        }
    }
}