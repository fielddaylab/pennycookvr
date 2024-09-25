using System;
using BeauUtil;
using FieldDay;
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
        public float MinRaycastDistance = 2;

        [Header("Timing Config")]
        public SignalLatchWindow LookLatch = SignalLatchWindow.Full;
        public SignalEnvelope LookEnvelope = new SignalEnvelope(2.5f, 3);
        public float HeldObjectLookMultiplier = 2f;

        [NonSerialized] public PlayerLookRecord CurrentLook;
        [NonSerialized] public RingBuffer<PlayerLookRecord> DecayingLook;

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

    static public class PlayerLookUtility {
        // iteration count for spherecast
        private const int IterationCount = 8;

        // TODO: This may need to be more nuanced
        static public LookTag FindBestLookTargetAlongRay(Ray ray, LayerMask mask, float raySize, float minDistance, float maxDistance, out float outDist) {
            // iterative
            float distanceSeg = maxDistance / IterationCount;
            for (int i = 0; i < IterationCount; i++) {
                float size = raySize * (distanceSeg * (i + 0.5f) / minDistance);
                float distance = distanceSeg + i * size;
                Ray r = new Ray(ray.GetPoint(distanceSeg * i - i * size), ray.direction);
                if (Physics.SphereCast(r, size, out RaycastHit hit, distance, mask, QueryTriggerInteraction.Collide)) {
                    //DebugDraw.AddLine(r.origin, hit.point, Color.red.WithAlpha(0.2f), size * 2f, 0.1f, false);
                    LookTag tag = hit.collider.GetComponent<LookTag>();
                    Rigidbody body;
                    if (!tag && (body = hit.rigidbody)) {
                        tag = body.GetComponent<LookTag>();
                    } else {
                        tag = hit.collider.GetComponentInParent<LookTag>();
                    }
                    if (tag) {
                        outDist = distance + hit.distance;
                        return tag;
                    }
                } else {
                    //DebugDraw.AddLine(r.origin, r.GetPoint(distanceSeg), Color.blue.WithAlpha(0.2f), size * 2f, 0.1f, false);
                }
            }
            outDist = -1;
            return null;
        }
    }
}