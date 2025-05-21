using FieldDay;
using FieldDay.HID.XR;
using FieldDay.Systems;
using FieldDay.VRHands;
using FieldDay.XR;
using UnityEngine;

namespace Pennycook {
    [SysUpdate(GameLoopPhase.FixedUpdate, 0)]
    public class PlayerHandGripSystem : ComponentSystemBehaviour<PlayerHandRig> {
        public override void ProcessWork(float deltaTime) {
            XRInputState input = Find.State<XRInputState>();
            PlayerMovementState moveState = Find.State<PlayerMovementState>();
            if (moveState.CurrentState == PlayerMovementState.State.Warping) {
				if(moveState.CurrentWarp.Group == Pennycook.Tablet.TabletWarpPointGroup.Tent) {
					foreach(var c in m_Components) {
						if (c.Grabber.State == GrabberState.Holding) {
							if(c.Grabber.HeldObject != null) {
								FieldDay.Sockets.Socketable s = c.Grabber.HeldObject.GetComponent<FieldDay.Sockets.Socketable>();
								if(s == null) {
									//drop current item...
									c.Grabber.State = GrabberState.AttemptRelease;
								}
								else {
									if(s.SocketType != FieldDay.Sockets.SocketFlags.Margo) {
										c.Grabber.State = GrabberState.AttemptRelease;
									}
								}
							}
						}
					}
				}
                return;
            }

            foreach(var c in m_Components) {
                ref XRHandState hand = ref input.Hand(c.Hand);

                if (c.Grabber.State == GrabberState.Empty) {
                    if (hand.Buttons.ConsumePress(XRHandButtons.GripButton)) {
                        c.Grabber.State = GrabberState.AttemptGrab;
                        VRGame.Events.Dispatch(GameEvents.PlayerGrab, EvtArgs.Create(new Data.GrabLogInfo(hand.Pose.position, 
                            hand.Pose.rotation, false, (Data.HandType)c.Hand)));
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
                        VRGame.Events.Dispatch(GameEvents.PlayerRelease, EvtArgs.Create(new Data.GrabLogInfo(hand.Pose.position, 
                            hand.Pose.rotation, c.Grabber.HeldObject.TapToRelease, (Data.HandType)c.Hand)));
                    }
                }
            }
        }
    }
}