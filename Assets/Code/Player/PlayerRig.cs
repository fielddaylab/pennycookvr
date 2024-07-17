using FieldDay.Physics;
using FieldDay.SharedState;
using FieldDay.VRHands;
using UnityEngine;

namespace Pennycook {
    public class PlayerRig : SharedStateComponent {
        public Transform MoveRoot;
        public Transform HeadRoot;

        public PlayerHandRig LeftHand;
        public PlayerHandRig RightHand;
    }

    static public class PlayerRigUtils {
        static public void SyncPhysicsHand(PlayerHandRig hand) {
            GrabUtility.DropCurrent(hand.Grabber, false);
            RBInterpolatorUtility.SyncInstant(hand.Interpolator);
        }

        static public void SyncPhysicsHands(PlayerRig rig) {
            SyncPhysicsHand(rig.LeftHand);
            SyncPhysicsHand(rig.RightHand);
        }
    }
}