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
            VRGame.Events.Dispatch(GameEvents.DayCompleted, state.DayIndex);
            if (state.DayIndex + 1 == 6) {
                state.DayIndex = 0;
                return Leaf_LoadDay(thread, state.DayIndex);
            } else {
                return Leaf_LoadDay(thread, state.DayIndex + 1);
            }
        }

        [LeafMember("DayBegin")]
        static public void DayBegin(int dayIndex) {
            Data.PennycookAnalytics pa = Find.State<Data.PennycookAnalytics>();
            if(pa) {
                pa.LogDayBegin(dayIndex);
            }
        }

        [LeafMember("GetCulturalVersion")]
        static public int GetCulturalVersion(bool forceReRandom) {
            PlayerProgressState pps = Find.State<PlayerProgressState>();
            if(pps.CulturalVersion == -1 || forceReRandom) {
                pps.CulturalVersion = (int)Random.Range(0,3);
                //Debug.Log("Cultural Version: " + pps.CulturalVersion);
            }
            return pps.CulturalVersion;
        }

        static public int GetScriptVersion()
        {
            PlayerProgressState pps = Find.State<PlayerProgressState>();
            //Debug.Log(pps.DayIndex);
            if (pps.DayIndex == 0)
            {
                pps.ScriptVersion = (int)Random.Range(0, 3);
            }

            return pps.ScriptVersion;
        }

        static public IEnumerator LoadDay(int dayIndex)
        {
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
            LoadGoals(config);
            while(GameLoop.IsLoading) {
                yield return null;
            }
        }

        static public void LoadGoals(DayConfigAsset config) {

            Tablet.TabletUtility.Goals.DayGoals = new Tablet.TabletGoal[config.Goals.Length];

            for (int i = 0; i < config.Goals.Length; ++i)
            {
                Tablet.TabletUtility.Goals.DayGoals[i] = new Tablet.TabletGoal();
                Tablet.TabletUtility.Goals.DayGoals[i].ID = config.Goals[i].ID;
                Tablet.TabletUtility.Goals.DayGoals[i].Type = config.Goals[i].Type;
                Tablet.TabletUtility.Goals.DayGoals[i].WarpPoint = config.Goals[i].WarpPoint;
				Tablet.TabletUtility.Goals.DayGoals[i].Instructions = config.Goals[i].Instructions;
				Tablet.TabletUtility.Goals.DayGoals[i].Summary = config.Goals[i].SummaryDescription;
                Tablet.TabletUtility.Goals.DayGoals[i].Completed = false;
                Tablet.TabletUtility.Goals.DayGoals[i].Current = false;
                if (config.Goals[i].SubGoals.Length > 0)
                {
                    Tablet.TabletUtility.Goals.DayGoals[i].SubGoals = new Tablet.TabletSubGoal[config.Goals[i].SubGoals.Length];
                    for (int j = 0; j < config.Goals[i].SubGoals.Length; ++j)
                    {
                        Tablet.TabletUtility.Goals.DayGoals[i].SubGoals[j] = new Tablet.TabletSubGoal();
                        Tablet.TabletUtility.Goals.DayGoals[i].SubGoals[j].Id = config.Goals[i].SubGoals[j].ID;
                        Tablet.TabletUtility.Goals.DayGoals[i].SubGoals[j].Text = config.Goals[i].SubGoals[j].Description;
                        Tablet.TabletUtility.Goals.DayGoals[i].SubGoals[j].Color = config.Goals[i].SubGoals[j].Color;
                    }
                }
                //Tablet.TabletUtility.Goals.DayGoals[i] = newGoal;
            }
        }

        static public DayConfigAsset GetConfigForDay(int dayIndex) {
            UniverseAsset univ = Find.GlobalAsset<UniverseAsset>();
            if (dayIndex >= univ.Days.Length)
            {
                dayIndex = 0;
            }
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
                        state.DayIndex = 0;
                    }

                    configId = univ.Days[state.DayIndex];
                    
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