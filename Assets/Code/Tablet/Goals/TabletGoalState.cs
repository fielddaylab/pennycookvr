using System;
using System.Collections.Generic;
using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using Leaf.Runtime;
using UnityEngine;

namespace Pennycook.Tablet {
    public sealed class TabletGoalState : SharedStateComponent {
        #region Inspector

        public List<SidePanelDisplay> SidePanels = new List<SidePanelDisplay>(4);

        public List<TabletGoal> DayGoals = new List<TabletGoal>();

        #endregion // Inspector

        static public readonly int MaxGoals = 6;

        [NonSerialized] public HashSet<StringHash32> RelevantCaptureIds = SetUtils.Create<StringHash32>(4);
    }

    public enum TabletGoalType {
        Scan,
        Warp,
        Capture,
        Count
    }

    public struct TabletSubGoal {
        public StringHash32 Id;
        public string Text;
        public Color Color;
        public bool Completed;
    }

    public struct TabletGoal {
        public bool Completed;
        public bool Current;

        public string Summary;
        public string Instructions;

        public TabletGoalType Type;
        public TabletWarpPointGroup WarpPoint;
        public TabletSubGoal[] SubGoals;
    }

    static public partial class TabletUtility {
        [SharedStateReference]
        static public TabletGoalState Goals { get; set; }
        
        //this should load the UI elements relevant to the goal, but not necessarily show them yet
        //unless the current tablet mode matches...
        static public void LoadGoals(TabletWarpPointGroup warpPointType) {
            //Debug.Log("IN LOAD GOALS");
            //Debug.Log("warpPointType: " + warpPointType);
            for (int i = 0; i < Goals.DayGoals.Count; ++i) {
                //Debug.Log("Loaded: " + Goals.DayGoals[i].Loaded);
                //Debug.Log("Warp Point: " + Goals.DayGoals[i].WarpPoint);
                if(Goals.DayGoals[i].WarpPoint == warpPointType) {
                    
                    SidePanelDisplay s = Goals.SidePanels[(int)Goals.DayGoals[i].Type];
                    if(s.Instructions != null) {
                        s.Instructions.text = Goals.DayGoals[i].Instructions;
                    }

                    if(s.Summary != null) { 
                        s.Summary.text = Goals.DayGoals[i].Summary;
                    }

                    if (Goals.DayGoals[i].SubGoals != null) {
                        for (int j = 0; j < Goals.DayGoals[i].SubGoals.Length; ++j) {
                            s.UIElements[j].gameObject.SetActive(true);
                            s.UIElements[j].Text.text = Goals.DayGoals[i].SubGoals[j].Text;
                            //s.UIElements[j].Circle.Color = Goals.DayGoals[i].SubGoals[j].Color;
                            if(Goals.DayGoals[i].Type == TabletGoalType.Capture) {
                                Goals.RelevantCaptureIds.Add(Goals.DayGoals[i].SubGoals[j].Id);
                            }
                        }
                    }
                }
            }
        }

        static public bool GoalOfTypeExists(TabletGoalType Type) {
            if(Goals != null && Goals.DayGoals != null) {
                for(int i = 0; i < Goals.DayGoals.Count; ++i) {
                    if(Goals.DayGoals[i].Type == Type) {
                        return true;
                    }
                }
            }
            return false;
        }

        [LeafMember("CompleteGoal")]
        static private bool LeafCompleteGoal(StringHash32 id) {
            TabletToolState currTool = Find.State<TabletToolState>();
            for(int i = 0; i < Goals.DayGoals.Count; ++i) {
                if(Goals.DayGoals[i].Type == (TabletGoalType)currTool.CurrentToolIndex) {
                    for(int j = 0; j < Goals.DayGoals[i].SubGoals.Length; ++j) {
                        if(Goals.DayGoals[i].SubGoals[j].Id == id) {
                            Goals.DayGoals[i].SubGoals[j].Completed = true;
                            if (Goals.SidePanels[currTool.CurrentToolIndex].UIElements[j].Check != null) {
                                Goals.SidePanels[currTool.CurrentToolIndex].UIElements[j].Check.SetAlpha(1);
                            }
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        [LeafMember("ClearGoals")]
        static private void LeafClearGoals() {
            TabletToolState currTool = Find.State<TabletToolState>();
            for(int i = 0; i < Goals.SidePanels.Count; ++i) {
                for(int j = 0; j < Goals.SidePanels[i].UIElements.Length; ++j) {
                    
                    if(Goals.SidePanels[i].Type == TabletGoalType.Scan || Goals.SidePanels[i].Type == TabletGoalType.Capture) {
                        if (Goals.SidePanels[i].UIElements[j].Check != null) {
                            Goals.SidePanels[i].UIElements[j].Check.SetAlpha(0);
                        }
                        Goals.SidePanels[i].UIElements[j].gameObject.SetActive(false);
                    }

                    if(Goals.SidePanels[i].Type != (TabletGoalType)currTool.CurrentToolIndex) {
                        Goals.SidePanels[i].SetState(false, false);
                    }
                }
            }
        }
    }
}