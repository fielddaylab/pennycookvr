using System.Collections;
using System.Collections.Generic;
using BeauUtil;
using FieldDay;
using FieldDay.Components;
using FieldDay.Sockets;
using UnityEngine;
using Pennycook.Tablet;

namespace Pennycook {
    public class PelicanCase : BatchedComponent , IRegistrationCallbacks {
        #region Inspector
        public ObjectSocket MargoSocket;
        public ObjectSocket ExteriorMargoSocket;

        //public GameObject ExteriorGroup;
        #endregion

        void IRegistrationCallbacks.OnRegister() {
            Game.Scenes.QueueOnEnable(this, SocketMargo);

            VRGame.Events.Register<TabletWarpPoint>(GameEvents.WarpPointUpdated, OnWarpUpdated);
        }
        void IRegistrationCallbacks.OnDeregister() {
            VRGame.Events?.Deregister<TabletWarpPoint>(GameEvents.WarpPointUpdated, OnWarpUpdated);
        }

        private void SocketMargo() {
            TabletControlState tcs = Find.State<TabletControlState>();
            if(MargoSocket) {
                SocketUtility.SetHomeSocket(tcs.Socketable, MargoSocket);
                SocketUtility.TryAddToSocket(tcs.Socketable, MargoSocket, false);
            }
        }

        private void OnWarpUpdated(TabletWarpPoint warpPoint) {
            Transform objPos = warpPoint.TabletCaseLocation;
            if (objPos) {
                objPos.GetPositionAndRotation(out Vector3 newPos, out Quaternion newRot);
                transform.SetPositionAndRotation(newPos, newRot);
            }

            /*TabletControlState tcs = Find.State<TabletControlState>();

           bool isInTent = warpPoint.Group == TabletWarpPointGroup.Tent;
            bool tabletWasAttached = tcs.Socketable.CurrentSocket == MargoSocket || tcs.Socketable.CurrentSocket == ExteriorMargoSocket;

            MargoSocket.gameObject.SetActive(isInTent);
            ExteriorGroup.SetActive(!isInTent);

            if (warpPoint.Group == TabletWarpPointGroup.Tent) {
                SocketUtility.SetHomeSocket(tcs.Socketable, MargoSocket);
                if (tabletWasAttached) {
                    SocketUtility.TryAddToSocket(tcs.Socketable, MargoSocket, false);
                }
            } else {
                SocketUtility.SetHomeSocket(tcs.Socketable, ExteriorMargoSocket);
                if (tabletWasAttached) {
                    SocketUtility.TryAddToSocket(tcs.Socketable, ExteriorMargoSocket, false);
                }
            }*/
        }
    }
}
