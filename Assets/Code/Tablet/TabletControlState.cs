using System;
using BeauUtil;
using FieldDay;
using FieldDay.Audio;
using FieldDay.HID.XR;
using FieldDay.Scripting;
using FieldDay.SharedState;
using FieldDay.Sockets;
using FieldDay.VRHands;
using FieldDay.XR;
using UnityEngine;

namespace Pennycook.Tablet {
    [SharedStateInitOrder(-1)]
    public class TabletControlState : SharedStateComponent, IRegistrationCallbacks {
        public Grabbable Grabbable;
        public Socketable Socketable;
        public Transform AudioLocation;

        private Rigidbody CachedRB;

        [NonSerialized] public BitSet32 GrippedHandMask;

        void IRegistrationCallbacks.OnDeregister() {
            Grabbable.OnGrabbed.Deregister(OnGrabbed);
            Grabbable.OnReleased.Deregister(OnGrabReleased);
        }

        void IRegistrationCallbacks.OnRegister() {
            Grabbable.OnGrabbed.Register(OnGrabbed);
            Grabbable.OnReleased.Register(OnGrabReleased);
            CachedRB = GetComponent<Rigidbody>();

        }

        private void OnGrabbed(Grabber grabber, int snapIndex) {
            StringHash32 nodeName = GrabUtility.ResolveSnapNodeName(grabber);
            switch (grabber.Chirality) {
                case XRHandIndex.Left: {
                    if (nodeName == "LeftHandle") {
                        GrippedHandMask.Set((int) XRHandIndex.Left);
                    }
                    break;
                }
                case XRHandIndex.Right: {
                    if (nodeName == "RightHandle") {
                        GrippedHandMask.Set((int) XRHandIndex.Right);
                    }
                    break;
                }
            }

            if (GrippedHandMask.Count > 0) {
                ScriptUtility.Trigger(TabletTriggers.LiftedTablet);
                if(CachedRB != null) {
                    CachedRB.angularDrag = 70f;
                    CachedRB.drag = 70f;
                    CachedRB.mass = 5f;
                }
            }
        }

        private void OnGrabReleased(Grabber grabber, int snapIndex) {
            if (grabber.Chirality != XRHandIndex.Any) {
                GrippedHandMask.Unset((int) grabber.Chirality);

                if (GrippedHandMask.IsEmpty) {
                    if(CachedRB != null) {
                        CachedRB.angularDrag = 1f;
                        CachedRB.drag = 1f;
                        CachedRB.mass = 10f;
                    }
                    ScriptUtility.Trigger(TabletTriggers.DroppedTablet);
                }
            }
        }
    }

    static public partial class TabletUtility {
        static public void PlayHaptics(float amp, float duration) {
            TabletControlState ctrl = Find.State<TabletControlState>();
            if (ctrl.GrippedHandMask.IsSet((int) XRHandIndex.Left)) {
                PlayerHaptics.Play(XRHandIndex.Left, amp, duration);
            }
            if (ctrl.GrippedHandMask.IsSet((int) XRHandIndex.Right)) {
                PlayerHaptics.Play(XRHandIndex.Right, amp, duration);
            }
        }

        static public bool ConsumeButtonPress(XRHandButtons buttons) {
            TabletControlState ctrl = Find.State<TabletControlState>();
            XRInputState input = Find.State<XRInputState>();
            if (ctrl.GrippedHandMask.IsSet((int) XRHandIndex.Left)) {
                if (input.LeftHand.Buttons.ConsumePress(buttons)) {
                    return true;
                }
            }
            if (ctrl.GrippedHandMask.IsSet((int) XRHandIndex.Right)) {
                if (input.RightHand.Buttons.ConsumePress(buttons)) {
                    return true;
                }
            }
            return false;
        }

        static public bool IsButtonHeld(XRHandButtons buttons) {
            TabletControlState ctrl = Find.State<TabletControlState>();
            XRInputState input = Find.State<XRInputState>();
            if (ctrl.GrippedHandMask.IsSet((int) XRHandIndex.Left)) {
                if (input.LeftHand.Buttons.IsDown(buttons)) {
                    return true;
                }
            }
            if (ctrl.GrippedHandMask.IsSet((int) XRHandIndex.Right)) {
                if (input.RightHand.Buttons.IsDown(buttons)) {
                    return true;
                }
            }
            return false;
        }

        static public void PlaySfx(StringHash32 sfxId) {
            Sfx.Play(sfxId, Find.State<TabletControlState>().AudioLocation);
        }
    }
}