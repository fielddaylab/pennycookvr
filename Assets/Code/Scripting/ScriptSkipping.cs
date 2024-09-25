using BeauUtil;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.HID.XR;
using FieldDay.Scripting;
using FieldDay.Sockets;
using FieldDay.VRHands;

namespace Pennycook {
    static public class ScriptSkipping {
        [InvokeOnBoot]
        static private void Initialize() {
            GameLoop.OnDebugUpdate.Register(() => {
                if (DebugInput.IsPressed(XRHandIndex.Right, XRHandButtons.PrimaryAxisClick)) {
                    ScriptUtility.ForEachThread((t) => {
                        t.SkipSingle();
                    });
                }
            });
        }
    }
}