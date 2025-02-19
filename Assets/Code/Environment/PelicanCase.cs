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
                SocketUtility.TryAddToSocket(tcs.Socketable, MargoSocket, false, false);
            }
        }

        private void OnWarpUpdated(TabletWarpPoint warpPoint) {
            Transform objPos = warpPoint.TabletCaseLocation;
            if (objPos) {
                objPos.GetPositionAndRotation(out Vector3 newPos, out Quaternion newRot);
                transform.SetPositionAndRotation(newPos, newRot);
            }
        }
    }
}
