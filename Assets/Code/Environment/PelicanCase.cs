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
        }
        void IRegistrationCallbacks.OnDeregister() {

        }

        private void SocketMargo() {
            TabletControlState tcs = Find.State<TabletControlState>();
            if(tcs) {
                Socketable s = tcs.GetComponent<Socketable>();
                if(s && MargoSocket) {
                    SocketUtility.TryAddToSocket(s, MargoSocket, false);
                }
            }
        }
    }
}
