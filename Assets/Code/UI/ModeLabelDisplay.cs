using FieldDay;
using UnityEngine;
using UnityEngine.UI;

namespace Pennycook {
    public class ModeLabelDisplay : MonoBehaviour {
        public LayoutOffset Offset;
        public RectTransform BG;

        [Header("Selected State")]
        public GameObject ExpandedContent;
        public Vector4 SelectedBGSize;

        [Header("Unselected State")]
        public Vector4 UnselectedBGSize;

        public void SetState(bool selected) {
            ExpandedContent.SetActive(selected);

            Vector2 offsetMin, offsetMax;
            Vector4 offsets = selected ? SelectedBGSize : UnselectedBGSize;

            offsetMin = new Vector2(offsets.x, offsets.y);
            offsetMax = new Vector2(offsets.z, offsets.w);

            BG.offsetMin = offsetMin;
            BG.offsetMax = offsetMax;
        }
    }
}