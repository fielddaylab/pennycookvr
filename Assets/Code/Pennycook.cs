using FieldDay;
using FieldDay.HID.XR;
using FieldDay.Scenes;
using FieldDay.Scripting;
using UnityEngine.SceneManagement;

namespace Pennycook {
    public class VRGame : Game {
        static public new EventDispatcher<EvtArgs> Events { get; private set; }

        [InvokePreBoot]
        static private void PreBoot() {
            Events = new EventDispatcher<EvtArgs>();
            SetEventDispatcher(Events);

            PlayerProgressState progress = new PlayerProgressState();
            Game.SharedState.Register(progress);
        }

        [InvokeOnBoot]
        static private void OnBoot() {
            XRUtility.SetRefreshRate(90);
            RaycastJobs.Initialize();
            Data.PennycookAnalytics pa = Find.State<Data.PennycookAnalytics>();
            if (pa)
            {
                pa.LogSessionStart();    
            }

            Game.Scenes.OnMainSceneReady.Register(() =>
            {
                ScriptUtility.Trigger(GameTriggers.SceneReady);
                PlayerProgressState state = Find.State<PlayerProgressState>();
                if (state != null)
                {
                    if (state.DayIndex == 0)
                    {
                        if (pa)
                        {
                            pa.LogStartGame();
                        }
                    }
                }
            });
            Game.Scenes.OnMainSceneLateEnable.Register(() => {
                ScriptUtility.Invoke(GameTriggers.ScenePrepare);
            });

            Game.Scenes.OnMainSceneUnloading.Register(() => {
                //ScriptUtility.Trigger(GameTriggers.SceneUnload);
                Pennycook.ScriptSocket.LeafClearHighlightSockets();
            });

            Find.State<PlayerProgressState>().Mode = UniverseUtility.GetModeForCurrentScene();

            GameLoop.OnShutdown.Register(() => {
                RaycastJobs.Shutdown();
            });
        }
    }
}