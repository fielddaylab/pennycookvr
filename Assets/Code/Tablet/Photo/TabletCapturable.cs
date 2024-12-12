using System;
using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.Components;
using FieldDay.Scripting;
using UnityEngine;

namespace Pennycook.Tablet {
    [RequireComponent(typeof(TabletHighlightable))]
    public sealed class TabletCapturable : BatchedComponent {
        public bool CanCapture = true;
        public SerializedHash32 CaptureId;
    }
}