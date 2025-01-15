using System;
using System.Collections.Generic;
using BeauUtil;
using FieldDay.Assets;
using FieldDay.Scenes;
using Leaf;
using UnityEngine;

namespace Pennycook {
    [CreateAssetMenu(menuName = "Pennycook/Day Config")]
    public sealed class DayConfigAsset : NamedAsset {
        public SceneReference Scene;
        public LeafAsset[] Scripts;
        public SceneReference[] AuxScenes;

        public GoalConfigAsset[] Goals;
    }
}