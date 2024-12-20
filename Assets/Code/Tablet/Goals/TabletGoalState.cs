using System;
using System.Collections.Generic;
using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using Leaf.Runtime;

namespace Pennycook.Tablet {
    public sealed class TabletGoalState : SharedStateComponent {
        #region Inspector

        public TabletCheckboxItem[] GoalItems;

        #endregion // Inspector

        static public readonly int MaxGoals = 6;

        [NonSerialized] public List<StringHash32> ActiveGoalIds = new List<StringHash32>();
        [NonSerialized] public BitSet32 GoalsComplete = new BitSet32();

        [NonSerialized] public HashSet<StringHash32> RelevantCaptureIds = SetUtils.Create<StringHash32>(4);
    }

    public struct TabletGoal {
        public bool Completed;
        public StringHash32 Id;
        public string Text;
    }

    static public partial class TabletUtility {
        [SharedStateReference]
        static private TabletGoalState Goals { get; set; }

        [LeafMember("CreateGoal")]
        static private void LeafCreateGoal(StringHash32 id, string text) {
            int index = Goals.ActiveGoalIds.Count;
            if (index >= TabletGoalState.MaxGoals) {
                throw new IndexOutOfRangeException("[LeafCreateGoal] Error: Goal '" + text + "' exceeded maximum number of " + TabletGoalState.MaxGoals);
            }
            Goals.ActiveGoalIds.Add(id);
            Goals.GoalItems[index].Text.SetText(text);
            Goals.GoalItems[index].Check.SetAlpha(0);
            Goals.GoalItems[index].gameObject.SetActive(true);
            Goals.GoalsComplete.Unset(index);
        }

        [LeafMember("CompleteGoal")]
        static private bool LeafCompleteGoal(StringHash32 id) {
            return LeafSetGoalComplete(id, true);
        }

        [LeafMember("SetGoalComplete")]
        static private bool LeafSetGoalComplete(StringHash32 id, bool complete) {
            int index = Goals.ActiveGoalIds.IndexOf(id);
            return SetGoalComplete(index, complete);
        }

        static public bool SetGoalComplete(int index, bool complete) {
            if (index < 0 || index >= Goals.GoalItems.Length)
                return false;
            Goals.GoalsComplete.Set(index);
            Goals.GoalItems[index].Check.SetAlpha(complete ? 1 : 0);
            return true;
        }

        [LeafMember("IsGoalComplete")]
        static private bool LeafCheckGoalComplete(StringHash32 id) {
            return Goals.GoalsComplete[Goals.ActiveGoalIds.IndexOf(id)];
        }

        [LeafMember("ClearGoals")]
        static private void LeafClearGoals() {
            Goals.ActiveGoalIds.Clear();
            Goals.GoalsComplete.Clear();
            for (int i = 0; i < Goals.GoalItems.Length; i++) {
                Goals.GoalItems[i].Text.SetText("Inactive");
                Goals.GoalItems[i].Check.SetAlpha(0);
                Goals.GoalItems[i].gameObject.SetActive(false);
            }
        }

        [LeafMember("WatchForBehavior")]
        static private void LeafWatchBehavior(StringHash32 id) {
            Goals.RelevantCaptureIds.Add(id);
        }

        [LeafMember("StopWatchingBehavior")]
        static private void LeafStopWatchingBehavior(StringHash32 id) {
            Goals.RelevantCaptureIds.Remove(id);
        }
    }
}