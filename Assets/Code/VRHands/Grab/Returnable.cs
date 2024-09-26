using System;
using System.Collections;
using System.Collections.Generic;
using BeauUtil;
using BeauRoutine;
using FieldDay.Components;
using FieldDay.Sockets;
using FieldDay.HID.XR;
using UnityEngine;


namespace FieldDay.VRHands {
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public class Returnable : BatchedComponent {
        #region Inspector

        #endregion
        [NonSerialized] public Rigidbody CachedRB;
        [NonSerialized] public Vector3 OriginalPosition;
        [NonSerialized] public Quaternion OriginalRotation;
        [NonSerialized] public Transform OriginalParent;

        private Routine ReturnProcess;

        private void Awake() {
            this.CacheComponent(ref CachedRB);

            OriginalPosition = transform.position;
            OriginalRotation = transform.rotation;
			OriginalParent = transform.parent;
        }

        public void OnCollisionEnter(Collision c) {
            int l = c.GetContact(0).otherCollider.gameObject.layer;
			if((l == 11 || l == 12) && !CachedRB.isKinematic) {
                if (!ReturnProcess.Exists()) {
                    ReturnProcess = Routine.Start(this, ReturnToStart());
				}
			}
		}

		private IEnumerator ReturnToStart() {
			yield return 1;
            if(TryGetComponent(out Socketable s)) {
                if(s.OriginalSocket) {
                    if(s.OriginalSocket.Current == null) {
                        //Debug.Log("Return 1");
                        SocketUtility.TryAddToSocket(s, s.OriginalSocket, true);
                    } /*else {
                        if(OriginalParent != null && (OriginalParent.gameObject != OriginalSocket.gameObject))
                        {
                            //Debug.Log("Return 2");
                            GrabUtility.ReturnToOriginalSpawnPoint(this);
                        }
                    }*/
                } else {
                    CachedRB.position = OriginalPosition;
                    CachedRB.rotation = OriginalRotation;
                }
            } else {
                CachedRB.position = OriginalPosition;
                CachedRB.rotation = OriginalRotation;
            }
		}
    }
}