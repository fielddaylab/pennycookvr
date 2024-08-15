using FieldDay;
using UnityEngine;
using UnityEngine.UI;

namespace Pennycook {
    public class ModeLabelDisplay : MonoBehaviour {
        public ColorPaletteTarget2 Targets;
        public LayoutOffset Offset;

        [Header("Selected State")]
        public GameObject ExpandedContent;
        public ColorPalette2 SelectedColors;
        public Vector2 SelectedOffset;

        [Header("Unselected State")]
        public ColorPalette2 UnselectedColors;
        public Vector2 UnselectedOffset;

        public void SetState(bool selected) {
            ExpandedContent.SetActive(selected);
            Offset.Offset2 = selected ? SelectedOffset : UnselectedOffset;
            ColorPalette.Apply(selected ? SelectedColors : UnselectedColors, Targets);
        }
    }
}