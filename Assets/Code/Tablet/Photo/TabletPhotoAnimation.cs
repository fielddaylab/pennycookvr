using UnityEngine;
using UnityEngine.UI;

namespace Pennycook.Tablet {
    public sealed class TabletPhotoAnimation : MonoBehaviour {
        public LayoutOffset Offset;
        public CanvasGroup Group;
        public RawImage PhotoRenderer;

        public Material DefaultMaterial;
        public Material FailureMaterial;
    }
}