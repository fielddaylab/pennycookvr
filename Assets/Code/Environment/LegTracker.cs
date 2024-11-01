using System.Collections;
using System.Collections.Generic;
using BeauUtil;
using FieldDay;
using FieldDay.Components;
using FieldDay.Sockets;
using UnityEngine;
using Pennycook.Tablet;
using FieldDay.VRHands;

namespace Pennycook {

    [RequireComponent(typeof(Grabbable))]
    public class LegTracker : BatchedComponent , IRegistrationCallbacks {

        static public readonly StringHash32 RemovedTracker = "TrackerRemoved";

        #region Inspector
        private Grabbable GrabComponent;

        //public GameObject ExteriorGroup;
        #endregion

        void IRegistrationCallbacks.OnRegister() {
            this.CacheComponent(ref GrabComponent);
        }
        void IRegistrationCallbacks.OnDeregister() {
            
        }
    }
}
