using System;
using BeauUtil;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.Filters;
using FieldDay.SharedState;
using FieldDay.VRHands;
using Pennycook.Tablet;
using UnityEngine;

namespace Pennycook {
    public sealed class PlayerLookTracker : SharedStateComponent, IRegistrationCallbacks {
        public Transform LookRoot;

        [Header("Raycast Config")]
        public float RaycastSize = 0.08f;
        public float RaycastDistance = 2;
        public int HitsPerRay = 4;
        public int RaycastResolution = 3;

        [Header("Timing Config")]
        public SignalLatchWindow LookLatch = SignalLatchWindow.Full;
        public SignalEnvelope LookEnvelope = new SignalEnvelope(2.5f, 3);
        public float HeldObjectLookMultiplier = 2f;

        [NonSerialized] public PlayerLookRecord CurrentLook;
        [NonSerialized] public RingBuffer<PlayerLookRecord> DecayingLook;

        [NonSerialized] public RaycastJob RaycastJob;

        void IRegistrationCallbacks.OnDeregister() {
        }

        void IRegistrationCallbacks.OnRegister() {
            DecayingLook = new RingBuffer<PlayerLookRecord>(16, RingBufferMode.Fixed);
        }
    }

    public struct PlayerLookRecord {
        public LookTag Object;
        public Grabbable Grabbable;
        public AnalogSignal Signal;
    }
}