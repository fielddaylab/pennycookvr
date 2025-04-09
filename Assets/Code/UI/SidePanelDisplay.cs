using FieldDay;
using FieldDay.Components;
using UnityEngine;
using UnityEngine.UI;

namespace Pennycook {
    public class SidePanelDisplay : BatchedComponent, IRegistrationCallbacks {

        public Tablet.TabletGoalType Type;
        public Tablet.TabletCheckboxItem[] UIElements;
        public SidePanelDisplay PairedPanel;

        public TMPro.TextMeshProUGUI Instructions;
        public TMPro.TextMeshProUGUI Summary;

        [Header("Selected State")]
        public bool Active;

        static int COUNT_METER_END = -140;
        static int COUNT_METER_START = 30;

        void IRegistrationCallbacks.OnRegister() {
            Tablet.TabletCountable.OnCounted.Register(CountIncreased);
        }

        void IRegistrationCallbacks.OnDeregister() {
            Tablet.TabletCountable.OnCounted.Deregister(CountIncreased);
        }

        public void SetState(bool visible, bool highlighted=false) {
            
            if(!visible && Type == Tablet.TabletGoalType.Count) {
                ResetCount();
            }

            gameObject.SetActive(visible);

            //todo - dim or brighten the UI elements.
            if (highlighted) {

            } else {

            }
        }

        public void CountIncreased(int currCount, int totalCount) {
            if(Type == Tablet.TabletGoalType.Count) {
                if(UIElements.Length > 0) {
                    Tablet.TabletCheckboxItem countMeter = UIElements[0];
                    BeauUtil.UI.RectGraphic bg = UIElements[0].Background;
                    RectTransform rt = bg.gameObject.GetComponent<RectTransform>();
                    if(currCount < totalCount) {
                        rt.offsetMax = new Vector2(-(COUNT_METER_START + (((float)currCount/(float)totalCount) * (COUNT_METER_END-COUNT_METER_START))), rt.offsetMax.y);
                        countMeter.Text.text = ((int)(((float)currCount / (float)totalCount) * 100f)).ToString() + "%";
                    } else {
                        rt.offsetMax = new Vector2(-COUNT_METER_END, rt.offsetMax.y);
                        countMeter.Text.text = "100%";
                    }
                }
            }
        }

        void ResetCount() {
            if (UIElements.Length > 0) {
                Tablet.TabletCheckboxItem countMeter = UIElements[0];
                BeauUtil.UI.RectGraphic bg = UIElements[0].Background;
                RectTransform rt = bg.gameObject.GetComponent<RectTransform>();
                rt.offsetMax = new Vector2(-COUNT_METER_START, rt.offsetMax.y);
                countMeter.Text.text = "0%";
            }
        }
    }
}