using BeauUtil;
using FieldDay.HID.XR;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace FieldDay.VRHands {
    [DisallowMultipleComponent]
    public class GrabbableSnapNode : MonoBehaviour {
        #region Inspector

        public XRHandIndex ValidHandType = XRHandIndex.Any;
        public bool IsDynamic;

        #endregion // Inspector

#if UNITY_EDITOR

        private void OnDrawGizmos() {
            Gizmos.color = Color.blue.WithAlpha(0.7f);
            Gizmos.DrawCube(transform.position, new Vector3(0.04f, 0.04f, 0.04f));
        }

#endif // UNITY_EDITOR
    }
}