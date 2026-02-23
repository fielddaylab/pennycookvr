using FieldDay;
using FieldDay.HID.XR;
using FieldDay.Scenes;
using FieldDay.Scripting;
using UnityEngine.SceneManagement;
using UnityEngine;

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
                            string assetStyle = "";
                            string scriptStyle = "";

                            if (state.CulturalVersion == 0)
                            {
                                assetStyle = "DEFAULT";
                            }
                            else if (state.CulturalVersion == 1)
                            {
                                assetStyle = "MEXICAN_AMERICAN";
                            }
                            else if (state.CulturalVersion == 2)
                            {
                                assetStyle = "MIDDLE_AMERICAN";
                            }

                            if (state.ScriptVersion == 0)
                            {
                                scriptStyle = "DEFAULT";
                            }
                            else if (state.ScriptVersion == 1)
                            {
                                scriptStyle = "MEXICAN_AMERICAN";
                            }
                            else if (state.ScriptVersion == 2)
                            {
                                scriptStyle = "MIDWEST";
                            }

                            //Debug.Log(assetStyle + " " + scriptStyle);

                            pa.LogStartGame(assetStyle, scriptStyle);
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