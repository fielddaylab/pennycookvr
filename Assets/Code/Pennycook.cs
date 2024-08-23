using FieldDay;
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
            Game.Scenes.OnMainSceneReady.Register(() => {
                ScriptUtility.Trigger(GameTriggers.SceneReady);
            });
        }
    }
}