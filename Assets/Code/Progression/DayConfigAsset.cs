using BeauUtil;
using FieldDay.Assets;
using Leaf;
using UnityEngine;

namespace Pennycook {
    [CreateAssetMenu(menuName = "Pennycook/Day Config")]
    public sealed class DayConfigAsset : NamedAsset {
        public SceneReference Scene;
        public LeafAsset[] Scripts;
    }
}