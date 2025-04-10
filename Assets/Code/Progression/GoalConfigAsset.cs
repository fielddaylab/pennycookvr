using System;
using System.Collections.Generic;
using BeauUtil;
using FieldDay.Assets;
using FieldDay.Scenes;
using Leaf;
using UnityEngine;

namespace Pennycook {

    [CreateAssetMenu(menuName = "Pennycook/Goal Config")]
    public sealed class GoalConfigAsset : NamedAsset {
        public string ID;
        public Tablet.TabletWarpPointGroup WarpPoint;
        public Tablet.TabletGoalType Type;
        public string Instructions;
        public string SummaryDescription;
        public SubGoalConfigAsset[] SubGoals;
    }
}