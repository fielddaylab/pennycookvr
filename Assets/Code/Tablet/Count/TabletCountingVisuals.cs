using FieldDay.Animation;
using UnityEngine;

namespace Pennycook.Tablet {
    public sealed class TabletCountingVisuals : MonoBehaviour {
        public RectTransform RotatingRing;

        private void LateUpdate() {
            RotatingRing.Rotate(0, 0, 2f * Time.deltaTime);
        }
    }
}