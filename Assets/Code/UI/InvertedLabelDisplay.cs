using UnityEngine;
using UnityEngine.UI;

namespace Pennycook {
    public class InvertedLabelDisplay : MonoBehaviour {
        public Graphic Content;
        public LayoutOffset Offset;

        [Header("Selected State")]
        public Graphic SelectedBackground;
        public Color SelectedContentColor;
        public Vector2 SelectedOffset;

        [Header("Unselected State")]
        public GameObject UnselectedOutline;
        public Color UnselectedContentColor;
        public Vector2 UnselectedOffset;

        public void SetState(bool selected) {
            Content.color = selected ? SelectedContentColor : UnselectedContentColor;
            SelectedBackground.enabled = selected;
            UnselectedOutline.SetActive(!selected);
            Offset.Offset2 = selected ? SelectedOffset : UnselectedOffset;
        }
    }
}