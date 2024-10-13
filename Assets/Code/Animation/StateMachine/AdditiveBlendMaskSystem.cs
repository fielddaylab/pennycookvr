using System;
using System.Collections.Generic;
using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.Animation;
using FieldDay.Components;
using FieldDay.Scenes;
using FieldDay.Systems;
using UnityEngine;

namespace Pennycook.Animation {
    [SysUpdate(GameLoopPhase.LateUpdate, -1000)]
    public sealed class AdditiveBlendMaskSystem : ComponentSystemBehaviour<AdditiveBlendMaskState> {
        public override void ProcessWork(float deltaTime) {
            foreach(var component in m_Components) {
                if (!component.Animator.isActiveAndEnabled) {
                    continue;
                }

                int layerCount = component.LayerCount;
                ProcessInterpolations(ref component.ScriptWeights, layerCount, deltaTime);
                ProcessInterpolations(ref component.StateMachineWeights, layerCount, deltaTime);

                Float8 finalWeights = new Float8();
                for (int i = 0; i < layerCount; i++) {
                    finalWeights[i] = component.ScriptWeights.Current[i] * component.StateMachineWeights.Current[i];
                }

                ApplyBlend(component, finalWeights);
            }
        }

        static private void ProcessInterpolations(ref AdditiveBlendMaskState.LerpState lerps, int count, float deltaTime) {
            float target;
            for (int i = 0; i < count; i++) {
                ref float current = ref lerps.Current[i];
                target = lerps.Target[i];
                current = Mathf.Lerp(current, target, TweenUtil.Lerp(lerps.Lerp[i], 1, deltaTime));
                if (Mathf.Approximately(current, target)) {
                    current = target;
                }
            }
        }

        static private void ApplyBlend(AdditiveBlendMaskState state, Float8 weights) {
            for (int i = 0; i < state.LayerCount; i++) {
                if (state.LastAppliedWeights[i] != weights[i]) {
                    state.Animator.SetLayerWeight(i, weights[i]);
                }
            }
            state.LastAppliedWeights = weights;
        }
    }
}