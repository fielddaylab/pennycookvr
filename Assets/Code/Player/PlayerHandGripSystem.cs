using FieldDay;
using FieldDay.HID.XR;
using FieldDay.Systems;
using FieldDay.VRHands;
using FieldDay.XR;

namespace Pennycook {
    [SysUpdate(GameLoopPhase.FixedUpdate, 0)]
    public class PlayerHandGripSystem : ComponentSystemBehaviour<PlayerHandRig> {
        public override void ProcessWork(float deltaTime) {
            XRInputState input = Find.State<XRInputState>();
            PlayerMovementState moveState = Find.State<PlayerMovementState>();
            if (moveState.CurrentState == PlayerMovementState.State.Warping) {
                return;
            }

            foreach(var c in m_Components) {
                ref XRHandState hand = ref input.Hand(c.Hand);

                if (c.Grabber.State == GrabberState.Empty) {
                    if (hand.Buttons.ConsumePress(XRHandButtons.GripButton)) {
                        c.Grabber.State = GrabberState.AttemptGrab;
                    }
                } else if (c.Grabber.State == GrabberState.Holding) {
                    bool release;
                    if (c.Grabber.HeldObject.TapToRelease) {
                        release = hand.Buttons.ConsumePress(XRHandButtons.GripButton);
                    } else {
                        release = !hand.Buttons.IsDown(XRHandButtons.GripButton);
                    }

                    if (release) {
                        c.Grabber.State = GrabberState.AttemptRelease;
                    }
                }
            }
        }
    }
}