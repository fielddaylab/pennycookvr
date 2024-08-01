using Pennycook.Tablet;
using UnityEngine;

namespace Pennycook.Sandbox {
    [RequireComponent(typeof(TabletInteractable), typeof(Rigidbody))]
    public class TestingBallInteraction : MonoBehaviour {
        public Vector3 Force;

        private void Awake() {
            GetComponent<TabletInteractable>().OnInteract.Register((i) => {
                GetComponent<Rigidbody>().AddForce(Force, ForceMode.Impulse);
                i.AddCooldown(0.2f);
            });
        }
    }
}