using BeauUtil;
using FieldDay;
using FieldDay.Audio;
using FieldDay.Debugging;
using FieldDay.HID.XR;
using FieldDay.Scripting;
using FieldDay.Sockets;
using FieldDay.VRHands;

namespace Pennycook {
    static public class ScriptSkipping {
        [InvokeOnBoot]
        static private void Initialize() {
            /*GameLoop.OnDebugUpdate.Register(() => {
                if (DebugInput.ConsumePress(XRHandIndex.Right, XRHandButtons.PrimaryAxisClick) || DebugInput.IsPressed(UnityEngine.KeyCode.K)) {
                    ScriptUtility.ForEachThread((t) => {
                        t.SkipSingle();
                    });
                }
                if (DebugInput.ConsumePress(XRHandIndex.Left, XRHandButtons.PrimaryAxisClick) || DebugInput.IsPressed(UnityEngine.KeyCode.L)) {
                    UniverseUtility.LoadNextDay();
                    ScriptUtility.KillAllThreads();
                    Sfx.StopAll();
                }
            });*/
        }
    }
}