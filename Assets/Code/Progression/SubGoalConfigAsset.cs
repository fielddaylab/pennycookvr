using System;
using System.Collections.Generic;
using BeauUtil;
using FieldDay.Assets;
using FieldDay.Scenes;
using Leaf;
using UnityEngine;

namespace Pennycook {

    [CreateAssetMenu(menuName = "Pennycook/Sub Goal Config")]
    public sealed class SubGoalConfigAsset : NamedAsset {
        public string ID;
        public string Description;
        public Color Color;
    }
}