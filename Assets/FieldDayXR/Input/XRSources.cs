using UnityEngine;
using UnityEngine.XR;

namespace FieldDay.XR {
    /// <summary>
    /// XR data source mask.
    /// </summary>
    public enum XRSourceMask : uint {
        None = 0,

        LeftEye = 1 << XRNode.LeftEye,
        RightEye = 1 << XRNode.RightEye,
        CenterEye = 1 << XRNode.CenterEye,
        Head = 1 << XRNode.Head,
        LeftHand = 1 << XRNode.LeftHand,
        RightHand = 1 << XRNode.RightHand
    }
}