using System.Collections;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Assets;
using FieldDay.Scenes;
using FieldDay.Scripting;
using Leaf;
using Leaf.Runtime;
using UnityEngine;

namespace Pennycook {
    [CreateAssetMenu(menuName = "Pennycook/Universe")]
    public sealed class UniverseAsset : GlobalAsset {
        [AssetName(typeof(DayConfigAsset))] public StringHash32 Title;
        [AssetName(typeof(DayConfigAsset))] public StringHash32[] Days;
        [AssetName(typeof(DayConfigAsset))] public StringHash32 Ending;
        [AssetName(typeof(DayConfigAsset))] public StringHash32 Sandbox;
    }

    static public class UniverseUtility {
        [LeafMember("LoadDay")]
        static private IEnumerator Leaf_LoadDay([BindThread] ScriptThread thread, int dayIndex) {
            IEnumerator wait = LoadDay(dayIndex);

            if (LeafRuntime.PredictTransition(thread)) {
                thread.Kill();
                return null;
            } else {
                return wait;
            }
        }

        [LeafMember("LoadNextDay")]
        static private IEnumerator Leaf_LoadNextDay([BindThread] ScriptThread thread) {
            var state = Find.State<PlayerProgressState>();
            return Leaf_LoadDay(thread, state.DayIndex + 1);
        }

        static public IEnumerator LoadDay(int dayIndex) {
            Find.State<PlayerProgressState>().DayIndex = dayIndex;
            return Routine.Start(GameLoop.Host, LoadDayRoutine(dayIndex)).Wait();
        }

        static public IEnumerator LoadNextDay() {
            var state = Find.State<PlayerProgressState>();
            return LoadDay(state.DayIndex + 1);
        }

        static private IEnumerator LoadDayRoutine(int dayIndex) {
            DayConfigAsset config = GetConfigForDay(dayIndex);
            Log.Msg("[UniverseUtility] Loading day '{0}'", config.name);
            Game.Scenes.LoadMainScene(config.Scene, true);
            while(Game.Scenes.IsMainLoading()) {
                yield return null;
            }
        }

        static public DayConfigAsset GetConfigForDay(int dayIndex) {
            UniverseAsset univ = Find.GlobalAsset<UniverseAsset>();
            StringHash32 configId = univ.Days[dayIndex];
            return Find.NamedAsset<DayConfigAsset>(configId);
        }

        static public DayConfigAsset GetConfigForCurrentState() {
            var state = Find.State<PlayerProgressState>();
            UniverseAsset univ = Find.GlobalAsset<UniverseAsset>();
            StringHash32 configId;
            switch (state.Mode) {
                case PlayerProgressMode.Gameplay:
                default:
                    if (state.DayIndex >= univ.Days.Length) {
                        configId = univ.Ending;
                    } else {
                        configId = univ.Days[state.DayIndex];
                    }
                    break;
                case PlayerProgressMode.Title: {
                    configId = univ.Title;
                    break;
                }
                case PlayerProgressMode.Sandbox: {
                    configId = univ.Sandbox;
                    break;
                }
            }
            return Find.NamedAsset<DayConfigAsset>(configId);
        }

        static public PlayerProgressMode GetModeForCurrentScene() {
            string sceneName = ScenesUtility.ActiveSceneName();
            if (sceneName.StartsWith("Exterior") || sceneName.StartsWith("Day")) {
                return PlayerProgressMode.Gameplay;
            } else if (sceneName.StartsWith("Sandbox")) {
                return PlayerProgressMode.Sandbox;
            } else {
                return PlayerProgressMode.Title;
            }
        }
    }
}