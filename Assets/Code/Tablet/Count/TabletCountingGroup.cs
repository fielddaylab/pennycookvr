using System;
using System.Collections;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Variants;
using FieldDay;
using FieldDay.Components;
using FieldDay.Scenes;
using FieldDay.Scripting;
using Leaf.Runtime;
using UnityEngine;
using UnityEngine.Scripting;

namespace Pennycook.Tablet {
    public sealed class TabletCountingGroup : ScriptActorComponent {
        [NonSerialized] public TabletCountingGroupState State = TabletCountingGroupState.Inactive;
        [NonSerialized] public int TotalInGroup;
        [NonSerialized] public HashSet<TabletCountable> CurrentlyCounted;

        public override void OnScriptRegister(ScriptActor actor) {
            base.OnScriptRegister(actor);

            CurrentlyCounted = new HashSet<TabletCountable>(64);
        }

        [LeafMember("ActivateCount"), Preserve]
        private void ActivateCount(int totalInGroup)
        {
            State = TabletCountingGroupState.InProgress;
            TotalInGroup = totalInGroup;
        }

        [LeafMember("CountInProgress"), Preserve]
        private bool CountInProgress()
        {
            return State == (TabletCountingGroupState.InProgress) && (CurrentlyCounted.Count < TotalInGroup);
        }

        [LeafMember("IsCountFinished"), Preserve]
        private bool CountFinished()
        {
            if(CurrentlyCounted.Count < TotalInGroup)
            {
                return false;
            }

            State = TabletCountingGroupState.Completed;
            
            using (var table = TempVarTable.Alloc()) {
                table.ActorInfo(Actor);
                ScriptUtility.Trigger(TabletTriggers.TabletCounted, table);
            }
            
            return true;
        }
    }

    public enum TabletCountingGroupState {
        Inactive,
        Available,
        InProgress,
        Completed
    }
}