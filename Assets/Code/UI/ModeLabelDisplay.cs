using FieldDay;
using FieldDay.Animation;
using FieldDay.UI.Animation;
using UnityEngine;
using UnityEngine.UI;

namespace Pennycook {
    public class ModeLabelDisplay : MonoBehaviour {
        public LayoutOffset Offset;
        public RectTransform BG;
        public BaseMeshEffect IconShadow;

        [Header("Selected State")]
        public GameObject ExpandedContent;

        public void SetState(bool selected) {
            ExpandedContent.SetActive(selected);
            BG.gameObject.SetActive(selected);
            IconShadow.enabled = !selected;

            if (selected) {
                PopAnim.Play(Offset, PopAnim.Default);
            }
        }
    }
}