using FieldDay;
using FieldDay.HID.XR;
using FieldDay.Scripting;

namespace Pennycook {
    public class VRGame : Game {
        static public new EventDispatcher<EvtArgs> Events { get; private set; }

        [InvokePreBoot]
        static private void PreBoot() {
            Events = new EventDispatcher<EvtArgs>();
            SetEventDispatcher(Events);
        }

        [InvokeOnBoot]
        static private void OnBoot() {
            XRUtility.SetRefreshRate(90);
            RaycastJobs.Initialize();

            Game.Scenes.OnMainSceneReady.Register(() => {
                ScriptUtility.Trigger(GameTriggers.SceneReady);
            });
            Game.Scenes.OnMainSceneLateEnable.Register(() => {
                ScriptUtility.Invoke(GameTriggers.ScenePrepare);
            });

            GameLoop.OnShutdown.Register(() => {
                RaycastJobs.Shutdown();
            });
        }
    }
}