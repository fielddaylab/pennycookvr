using System;
using BeauRoutine;
using BeauUtil;
using FieldDay.Components;
using UnityEngine;

namespace Pennycook.Tablet {
    [RequireComponent(typeof(TabletHighlightable))]
    public class TabletCapturable : BatchedComponent {
        public bool CanCapture = true;
        public SerializedHash32 CaptureId;
    }
}