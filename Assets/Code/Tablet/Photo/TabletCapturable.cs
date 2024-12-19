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
        
        [Range(0, 1)] public float ViewAlignmentThreshold = 0.5f;
    }

    public readonly struct CaptureRecord : IEquatable<CaptureRecord> {
        public readonly TabletCapturable Source;
        public readonly StringHash32 Captured;

        public CaptureRecord(TabletCapturable capturable, StringHash32 captureId) {
            Source = capturable;
            Captured = captureId;
        }

        public bool Equals(CaptureRecord other) {
            return object.ReferenceEquals(Source, other.Source)
                && Captured == other.Captured;
        }

        public override int GetHashCode() {
            return CompareUtils.GetHashCode(Source) << 5
                ^ CompareUtils.GetHashCode(Captured);
        }
    }
}