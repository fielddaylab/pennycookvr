using FieldDay.Components;
using UnityEngine;

namespace Pennycook.Tablet {
    public class TabletScannable : BatchedComponent {
        [Header("Components")]
        public Collider HighlightCollider;

        [Header("Contents")]
        public string ShortLabel;
        public string DetailedHeader;
        [Multiline] public string DetailedText;
    }
}