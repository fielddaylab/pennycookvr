using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.Filters;
using FieldDay.Scripting;
using FieldDay.Systems;
using FieldDay.VRHands;
using FieldDay.XR;
using UnityEngine;
using UnityEngine.XR;

namespace Pennycook {
    [SysUpdate(GameLoopPhaseMask.Update | GameLoopPhaseMask.LateUpdate, 10)]
    public sealed class PlayerLookTrackingSystem : SharedStateSystemBehaviour<PlayerLookTracker, PlayerRig, XRInputState> {

        public const int LookMask = LayerMasks.Solid_Mask | LayerMasks.Grabbable_Mask | LayerMasks.Default_Mask
            | LayerMasks.Highlightable_Mask | LayerMasks.Water_Mask | LayerMasks.PlayerHand_Mask
            | LayerMasks.LookTag_Mask | LayerMasks.PenguinBody_Mask;

        public override void ProcessWork(float deltaTime) {
            if (GameLoop.IsPhase(GameLoopPhase.Update)) {
                if (m_StateC.IsAvailable(XRNode.Head)) {
                    m_StateA.LookRoot.GetPositionAndRotation(out Vector3 cameraPos, out Quaternion cameraRot);
                    m_StateA.RaycastJob = RaycastJobs.SmoothConeCast(cameraPos, Geom.Forward(cameraRot), m_StateA.RaycastSize, m_StateA.RaycastDistance, m_StateA.RaycastResolution, LookMask, m_StateA.HitsPerRay, QueryTriggerInteraction.Collide);
                    RaycastJobs.Kick(ref m_StateA.RaycastJob);
                } else {
                    m_StateA.RaycastJob = default;
                }
                return;
            }

            LookTag currentFocus;
            if (m_StateA.RaycastJob.IsValid()) {
                currentFocus = RaycastJobs.Analyze<LookTag>(ref m_StateA.RaycastJob, out RaycastHit hit);

                //foreach (var raycast in m_StateA.RaycastJob.Raycasts) {
                //    DebugDraw.AddLine(raycast.from, raycast.from + raycast.direction * raycast.distance, Color.yellow, 0.01f);
                //}

                //foreach (var score in m_StateA.RaycastJob.ResultScores) {
                //    if (score.Index < 0) {
                //        break;
                //    }

                //    RaycastCommand raycast = m_StateA.RaycastJob.Raycasts[score.Index / m_StateA.RaycastJob.ResultsPerRaycast];
                //    RaycastHit anyHit = m_StateA.RaycastJob.Results[score.Index];
                //    DebugDraw.AddLine(raycast.from, anyHit.point, Color.green, 0.02f);
                //}

                m_StateA.RaycastJob.Clear();
            } else {
                currentFocus = null;
            }
            
            ref PlayerLookRecord currentLookRecord = ref m_StateA.CurrentLook;

            if (currentFocus != currentLookRecord.Object) {
                if (currentLookRecord.Object != null) {
                    Log.Msg("[PlayerLookTrackingSystem] Losing focus of '{0}' (frame {1})", currentLookRecord.Object.name, Frame.Index);
                    m_StateA.DecayingLook.PushBack(m_StateA.CurrentLook);
                    DebugDraw.AddSphere(currentLookRecord.Object.transform.position, 0.2f, ColorBank.DarkBlue.WithAlpha(0.4f), 0.5f, false);
                }

                if (currentFocus != null) {
                    bool foundExistingRecord = false;
                    for(int i = 0; i < m_StateA.DecayingLook.Count; i++) {
                        PlayerLookRecord rec = m_StateA.DecayingLook[i];
                        if (rec.Object == currentFocus) {
                            currentLookRecord = rec;
                            Log.Msg("[PlayerLookTrackingSystem] Restored focus on '{0}' (frame {1})", currentLookRecord.Object.name, Frame.Index);
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
                        Log.Msg("[PlayerLookTrackingSystem] New focus on '{0}' (frame {1})", currentLookRecord.Object.name, Frame.Index);
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

                DebugDraw.AddSphere(currentFocus.transform.position, 0.2f, ColorBank.Firebrick.WithAlpha(0.4f), 0.01f, false);

                if (AnalogSignal.Activate(ref currentLookRecord.Signal, deltaTime, m_StateA.LookLatch, attack)) {
                    using(var table = TempVarTable.Alloc()) {
                        table.ActorInfo(currentFocus.Actor);
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

                if (AnalogSignal.Deactivate(ref decayingSignal.Signal, deltaTime, m_StateA.LookLatch, m_StateA.LookEnvelope.Decay)) {
                    Log.Msg("[PlayerLookTrackingSystem] Player stopped looking at '{0}' ('{1}', '{2}')", decayingSignal.Object.name, decayingSignal.Object.Actor.Id, decayingSignal.Object.Actor.ClassName);
                }

                if (decayingSignal.Signal.Analog <= 0) {
                    m_StateA.DecayingLook.FastRemoveAt(i);
                    continue;
                }
            }
        }
    }
}