using System;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Filters;
using FieldDay.Scripting;
using FieldDay.SharedState;
using FieldDay.Systems;
using FieldDay.VRHands;
using FieldDay.XR;
using Pennycook.Tablet;
using UnityEngine;
using UnityEngine.XR;

namespace Pennycook {
    [SysUpdate(GameLoopPhase.LateUpdate, 10)]
    public sealed class PlayerLookTrackingSystem : SharedStateSystemBehaviour<PlayerLookTracker, PlayerRig, XRInputState> {
        public const int LookMask = LayerMasks.Solid_Mask | LayerMasks.Grabbable_Mask | LayerMasks.Default_Mask
            | LayerMasks.Highlightable_Mask | LayerMasks.Water_Mask | LayerMasks.PlayerHand_Mask
            | LayerMasks.LookTag_Mask;

        public override void ProcessWork(float deltaTime) {
            m_StateA.LookRoot.GetPositionAndRotation(out Vector3 cameraPos, out Quaternion cameraRot);
            
            Ray r = new Ray(cameraPos, Geom.Forward(cameraRot));
            LookTag currentFocus;
            if (m_StateC.IsAvailable(XRNode.Head)) { 
                currentFocus = PlayerLookUtility.FindBestLookTargetAlongRay(r, LookMask, m_StateA.RaycastSize, m_StateA.MinRaycastDistance, m_StateA.MinRaycastDistance + 30, out float objDist);
            } else {
                currentFocus = null;
            }

            ref PlayerLookRecord currentLookRecord = ref m_StateA.CurrentLook;

            if (currentFocus != currentLookRecord.Object) {
                if (m_StateA.CurrentLook.Object != null) {
                    m_StateA.DecayingLook.PushBack(m_StateA.CurrentLook);
                }

                if (currentFocus != null) {
                    bool foundExistingRecord = false;
                    for(int i = 0; i < m_StateA.DecayingLook.Count; i++) {
                        PlayerLookRecord rec = m_StateA.DecayingLook[i];
                        if (rec.Object == currentFocus) {
                            currentLookRecord = rec;
                            m_StateA.DecayingLook.FastRemoveAt(i);
                            foundExistingRecord = true;
                            break;
                        }
                    }

                    if (!foundExistingRecord) {
                        currentLookRecord = new PlayerLookRecord() {
                            Object = currentFocus,
                            Grabbable = currentFocus.GetComponent<Grabbable>(),
                            Signal = default
                        };
                    }
                } else {
                    currentLookRecord = default;
                }
            }

            if (currentFocus != null) {
                bool isHolding = (currentLookRecord.Grabbable && currentLookRecord.Grabbable.CurrentGrabberCount > 0);
                float attack = m_StateA.LookEnvelope.Attack;
                if (isHolding) {
                    attack /= m_StateA.HeldObjectLookMultiplier;
                }
                if (AnalogSignal.Activate(ref currentLookRecord.Signal, deltaTime, m_StateA.LookLatch, attack)) {
                    using(var table = TempVarTable.Alloc()) {
                        table.Set("objectId", currentFocus.Actor.Id);
                        table.Set("objectType", currentFocus.Actor.ClassName);
                        table.Set("isHeld", isHolding);
                        ScriptUtility.Trigger(GameTriggers.PlayerLookAtObject, table);
                    }
                    Log.Msg("[PlayerLookTrackingSystem] Player is looking at '{0}' ('{1}', '{2}')", currentFocus.name, currentFocus.Actor.Id, currentFocus.Actor.ClassName);
                }
            }

            // tick down

            for(int i = m_StateA.DecayingLook.Count - 1; i >= 0; i--) {
                ref var decayingSignal = ref m_StateA.DecayingLook[i];
                if (!decayingSignal.Object || !decayingSignal.Object.isActiveAndEnabled) {
                    m_StateA.DecayingLook.FastRemoveAt(i);
                    continue;
                }

                AnalogSignal.Deactivate(ref decayingSignal.Signal, deltaTime, m_StateA.LookLatch, m_StateA.LookEnvelope.Decay); 
                if (decayingSignal.Signal.Analog <= 0) {
                    m_StateA.DecayingLook.FastRemoveAt(i);
                    continue;
                }
            }
        }
    }
}