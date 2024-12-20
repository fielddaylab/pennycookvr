using System;
using BeauUtil;
using FieldDay;
using FieldDay.Audio;
using FieldDay.SharedState;
using FieldDay.Systems;
using UnityEngine;

namespace Pennycook {
    [SysUpdate(FieldDay.GameLoopPhase.UnscaledLateUpdate)]
    public sealed class AmbientMixSystem : SharedStateSystemBehaviour<AmbientMixState, PlayerRig> {
        public override void ProcessWork(float deltaTime) {
            if (!Frame.Interval(2)) {
                return;
            }

            Vector3 headPos = m_StateB.HeadRoot.position;
            headPos.y += 15;
            bool headInside = m_StateA.InteriorCollider.Raycast(new Ray(headPos, Vector3.down), out var _, 15f);
            bool doorOpen = m_StateA.DoorAnimator.GetBool("open");

            AmbientMixGroupState outdoorState = !headInside ? AmbientMixGroupState.Full : (doorOpen ? AmbientMixGroupState.Partial : AmbientMixGroupState.Occluded);
            AmbientMixGroupState indoorState = headInside ? AmbientMixGroupState.Full : (doorOpen ? AmbientMixGroupState.Partial : AmbientMixGroupState.Occluded);

            float transitionTime = m_StateA.OutdoorState == AmbientMixGroupState.Uninitialized || m_StateA.IndoorState == AmbientMixGroupState.Uninitialized ? 0 : m_StateA.TransitionTime;

            if (Ref.ReplaceEnum(ref m_StateA.OutdoorState, outdoorState)) {
                AmbientMixParams p = GetMixParams(m_StateA.OutdoorParams, outdoorState);
                Sfx.SetBusVolume(m_StateA.OutdoorBus, p.Volume, transitionTime);
                Sfx.SetBusLoPass(m_StateA.OutdoorBus, p.LoPass, transitionTime);
            }

            if (Ref.ReplaceEnum(ref m_StateA.IndoorState, indoorState)) {
                AmbientMixParams p = GetMixParams(m_StateA.IndoorParams, indoorState);
                Sfx.SetBusVolume(m_StateA.IndoorBus, p.Volume, transitionTime);
                Sfx.SetBusLoPass(m_StateA.IndoorBus, p.LoPass, transitionTime);
            }
        }

        static private unsafe AmbientMixParams GetMixParams(AmbientMixGroupParams group, AmbientMixGroupState state) {
            AmbientMixParams* asArr = (AmbientMixParams*) &group;
            return asArr[(int) state]; 
        }
    }
}