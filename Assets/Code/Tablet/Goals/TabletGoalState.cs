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

        public TabletGoal[] DayGoals;

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
        public StringHash32 ID;

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
        static public void LoadGoals(int toolIndex) {
            //Debug.Log("IN LOAD GOALS");
            //Debug.Log("warpPointType: " + warpPointType);
            if(Goals.DayGoals == null) { 
                return;
            }

            if(toolIndex == 1 || toolIndex == 3) {
                return;
            }

            Tablet.TabletWarpPointGroup currWarp = Tablet.TabletWarpPointGroup.Tent;
            PlayerMovementState moveState = Find.State<PlayerMovementState>();
            if(moveState.CurrentWarp != null) {
                currWarp = moveState.CurrentWarp.Group;
            }

            Goals.SidePanels[0].SetVisibleAll(false);
            Goals.SidePanels[2].SetVisibleAll(false);

            int currGoal = -1;

            for (int i = 0; i < Goals.DayGoals.Length; ++i) {
                if(Goals.DayGoals[i].Type != TabletGoalType.Count) {
                    if(!Goals.DayGoals[i].Completed) {
                        currGoal = i;
                        break;
                    }
                }
            }
            
            if(currGoal != -1) {
                for (int i = 0; i < Goals.DayGoals.Length; ++i) {
                    if(Goals.DayGoals[i].Type != TabletGoalType.Count && Goals.DayGoals[i].WarpPoint == currWarp) {
                        //if we moved to our current tool
                        if(i == currGoal) {
                            //if(Goals.DayGoals[i].Current && !Goals.DayGoals[i].Completed) {
                            SidePanelDisplay s = Goals.SidePanels[toolIndex];

                            if(s.Title != null) {
                                s.Title.SetActive(true);
                            }

                            if(s.InstructionObject != null) {
                                s.InstructionObject.SetActive(true);
                            }

                            if(s.Instructions != null) {
                                s.Instructions.text = Goals.DayGoals[i].Instructions;
                            }

                            if (Goals.DayGoals[i].SubGoals != null) {
                                //Debug.Log("LOADING SUBGOALS: " + Goals.DayGoals[i].SubGoals.Length);
                                for (int j = 0; j < Goals.DayGoals[i].SubGoals.Length; ++j) {
                                    if(j < s.UIElements.Length) {
                                        s.UIElements[j].gameObject.SetActive(true);
                                        if(!Goals.DayGoals[i].SubGoals[j].Completed) {
                                            s.UIElements[j].Text.text = Goals.DayGoals[i].SubGoals[j].Text;
                                            s.UIElements[j].Circle.color = Goals.DayGoals[i].SubGoals[j].Color;
                                            s.UIElements[j].Check.SetAlpha(0);
                                        } else {
                                            s.UIElements[j].Check.SetAlpha(1);
                                        }
                                        //s.UIElements[j].Circle.Color = Goals.DayGoals[i].SubGoals[j].Color;
                                        if(Goals.DayGoals[i].Type == TabletGoalType.Capture) {
                                            Goals.RelevantCaptureIds.Add(Goals.DayGoals[i].SubGoals[j].Id);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            //just show header if different panel
                            //but also check if completed...
                            //likewise if not current goal, but same tool type, just show summary from current tool.
                            if(Goals.DayGoals[i].Completed) {
                                SidePanelDisplay s = Goals.SidePanels[toolIndex];
                                if(s != null) {
                                    if(i < s.SummaryHeaders.Length) { 
                                        s.SummaryHeaders[i].Text.text = Goals.DayGoals[i].Summary;
                                        s.SummaryHeaders[i].gameObject.SetActive(true);
                                    }
                                }
                            }
                        }
                    }
                }
            } else {
                //all completed...
                for (int i = 0; i < Goals.DayGoals.Length; ++i) {
                    if(Goals.DayGoals[i].Completed && Goals.DayGoals[i].Type != TabletGoalType.Count) {
                        SidePanelDisplay s = Goals.SidePanels[toolIndex];
                        if(s.Title != null) {
                            s.Title.SetActive(true);
                        }

                        if(i < s.SummaryHeaders.Length) {
                            s.SummaryHeaders[i].gameObject.SetActive(true);
                            s.SummaryHeaders[i].Text.text = Goals.DayGoals[i].Summary;
                        }
                    }
                }
            }
        }

        static public bool GoalOfTypeExists(TabletGoalType Type) {
            if(Goals != null && Goals.DayGoals != null) {
                for(int i = 0; i < Goals.DayGoals.Length; ++i) {
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
            for(int i = 0; i < Goals.DayGoals.Length; ++i) {
                if(Goals.DayGoals[i].Type == TabletGoalType.Scan || Goals.DayGoals[i].Type == TabletGoalType.Capture) {
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

        [LeafMember("CompleteMainGoal")]
        static private bool LeafCompleteMainGoal(StringHash32 id) {
            TabletToolState currTool = Find.State<TabletToolState>();
            for(int i = 0; i < Goals.DayGoals.Length; ++i) {
                if(Goals.DayGoals[i].ID == id) {
                    Goals.DayGoals[i].Completed = true;

                    TabletGoalState goals = Find.State<TabletGoalState>();
                    TabletUtility.LoadGoals(currTool.CurrentToolIndex);
                    return true;
                }
            }

            return false;
        }

        [LeafMember("SetCurrentMainGoal")]
        static private bool LeafSetCurrentMainGoal(StringHash32 id) {
            TabletToolState currTool = Find.State<TabletToolState>();
            for(int i = 0; i < Goals.DayGoals.Length; ++i) {
                if(Goals.DayGoals[i].ID == id) {
                    Goals.DayGoals[i].Current = true;
                    return true;
                }
            }

            return false;
        }

        [LeafMember("RemoveGoal")]
        static private bool LeafRemoveGoal(StringHash32 id) {
            TabletToolState currTool = Find.State<TabletToolState>();
            for(int i = 0; i < Goals.DayGoals.Length; ++i) {
                if(Goals.DayGoals[i].Type == TabletGoalType.Scan || Goals.DayGoals[i].Type == TabletGoalType.Capture) {
                    for(int j = 0; j < Goals.DayGoals[i].SubGoals.Length; ++j) {
                        if(Goals.DayGoals[i].SubGoals[j].Id == id) {
                            Goals.DayGoals[i].SubGoals[j].Completed = false;
                            if (Goals.SidePanels[currTool.CurrentToolIndex].UIElements[j].Check != null) {
                                Goals.SidePanels[currTool.CurrentToolIndex].UIElements[j].Check.SetAlpha(0);
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

                for(int j = 0; j < Goals.SidePanels[i].SummaryHeaders.Length; ++j) {
                    if(Goals.SidePanels[i].Type == TabletGoalType.Scan || Goals.SidePanels[i].Type == TabletGoalType.Capture) {
                        Goals.SidePanels[i].SummaryHeaders[j].gameObject.SetActive(false);
                    }
                }
            }
        }
    }
}