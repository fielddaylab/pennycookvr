using System;
using System.Collections.Generic;
using BeauUtil;
using FieldDay.Animation;
using FieldDay.Components;
using FieldDay.Scenes;
using UnityEngine;

namespace Pennycook.Animation {
    [RequireComponent(typeof(Animator))]
    public sealed class AdditiveBlendMaskState : BatchedComponent, IScenePreload {
        #region Types

        public struct LerpState {
            public Float8 Current;
            public Float8 Target;
            public Float8 Lerp;
        }

        #endregion // Types

        [Required] public Animator Animator;
        public float DefaultLerpSpeed = 5;
        public float StateMachineMaskLerpSpeed = 20;

        [NonSerialized] public int LayerCount;
        [NonSerialized] public Float8 LastAppliedWeights;
        [NonSerialized] public LerpState ScriptWeights;
        [NonSerialized] public LerpState StateMachineWeights;

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            LayerCount = Animator.layerCount;
            for(int i = 0; i < LayerCount; i++) {
                ScriptWeights.Current[i] = Animator.GetLayerWeight(i);
            }
            ScriptWeights.Target = ScriptWeights.Current;
            ScriptWeights.Lerp = new Float8(LayerCount, DefaultLerpSpeed);

            StateMachineWeights.Current = new Float8(LayerCount, 1);
            StateMachineWeights.Target = StateMachineWeights.Current;
            StateMachineWeights.Lerp = new Float8(LayerCount, StateMachineMaskLerpSpeed);

            LastAppliedWeights = ScriptWeights.Current;
            return null;
        }
    }
}