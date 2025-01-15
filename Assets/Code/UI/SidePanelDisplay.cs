using FieldDay;
using FieldDay.Animation;
using FieldDay.UI.Animation;
using UnityEngine;
using UnityEngine.UI;

namespace Pennycook {
    public class SidePanelDisplay : MonoBehaviour {

        public Tablet.TabletGoalType Type;
        public Tablet.TabletCheckboxItem[] UIElements;
        public SidePanelDisplay PairedPanel;

        [Header("Selected State")]
        public bool Active;

        public void SetState(bool visible, bool highlighted=false) {
            gameObject.SetActive(visible);
            //todo - dim or brighten the UI elements.
            if(highlighted) {

            } else {

            }
        }
    }
}