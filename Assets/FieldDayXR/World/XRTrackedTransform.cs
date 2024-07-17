using FieldDay.Components;
using UnityEngine;
using UnityEngine.XR;

namespace FieldDay.XR {
    /// <summary>
    /// Syncs a transform with data from XRInputState.
    /// </summary>
    public class XRTrackedTransform : BatchedComponent {
        public bool TrackingEnabled = true;
        [Space]
        [Tooltip("Node to track")] public XRNode Node = XRNode.Head;
        [Tooltip("If set, this only gets updated right before rendering")] public bool RenderOnly;

        [Header("Offsets")]
        public Quaternion Rotation = Quaternion.identity;
    }
}