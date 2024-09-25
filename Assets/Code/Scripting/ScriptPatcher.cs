using BeauUtil;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.HID.XR;
using FieldDay.Scripting;
using FieldDay.Sockets;
using FieldDay.VRHands;

namespace Pennycook {
    static public class ScriptPatcher {
        [InvokePreBoot]
        static private void Initialize() {
            ScriptNode.PatchFunction = (n) => {
                if ((n.Flags & ScriptNodeFlags.Trigger) != 0) {
                    if (n.TargetId.IsEmpty) {
                        if (n.TriggerOrFunctionId == GameTriggers.PlayerLookAtObject) {
                            n.TargetId = "Player";
                        }
                    }
                }
            };
        }
    }
}