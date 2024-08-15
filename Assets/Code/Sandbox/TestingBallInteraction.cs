using FieldDay.Audio;
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

        private void OnCollisionEnter(Collision collision) {
            var contact = collision.GetContact(0);
            if (contact.impulse.magnitude > 1) {
                Sfx.PlayDetached("PhysicsImpact", contact.point, Quaternion.LookRotation(contact.normal));
            }
        }
    }
}