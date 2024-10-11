using System;
using BeauRoutine;
using FieldDay.Components;
using FieldDay.Systems;
using UnityEngine;

namespace Pennycook.Animation {
	public sealed class LookSmoothingSystem : ComponentSystemBehaviour<LookSmoothing> {
        static private int LookXParam;
        static private int LookYParam;

        public override void Initialize() {
            base.Initialize();

            LookXParam = Animator.StringToHash("LookX");
            LookYParam = Animator.StringToHash("LookY");
        }

        public override void ProcessWork(float deltaTime) {
            float targetX, targetY, newX, newY;

            foreach (var comp in m_Components) {
                if (comp.Mode == LookTargetMode.Disabled) {
                    continue;
                }

                switch (comp.Mode) {
                    case LookTargetMode.Forward:
                    default: {
                        targetX = targetY = 0;
                        break;
                    }

                    case LookTargetMode.ConstantLocal: {
                        targetX = comp.LookVector.x;
                        targetY = comp.LookVector.y;
                        break;
                    }

                    case LookTargetMode.ConstantWorld: {
                        Vector2 localVec = comp.WorldLookDirectionToLocal(comp.LookVector);
                        targetX = localVec.x;
                        targetY = localVec.y;
                        break;
                    }

                    case LookTargetMode.TowardsTransform: {
                        Vector3 towards = comp.LookTowards.position - comp.LookFrom.position;
                        Vector2 localVec = comp.WorldLookDirectionToLocal(towards.normalized);
                        targetX = localVec.x;
                        targetY = localVec.y;
                        break;
                    }
                }

                float lerpAmt = TweenUtil.Lerp(comp.LookLerpSpeed, 1, deltaTime);

                newX = Mathf.Lerp(comp.LastAppliedLook.x, targetX, lerpAmt);
                newY = Mathf.Lerp(comp.LastAppliedLook.y, targetY, lerpAmt);

                if (!Mathf.Approximately(newX, comp.LastAppliedLook.x)) {
                    comp.Animator.SetFloat(LookXParam, newX);
                    comp.LastAppliedLook.x = newX;
                }
                if (!Mathf.Approximately(targetY, comp.LastAppliedLook.y)) {
                    comp.Animator.SetFloat(LookYParam, newY);
                    comp.LastAppliedLook.y = newY;
                }

                bool wasConstant = comp.Mode == LookTargetMode.Forward || comp.Mode == LookTargetMode.ConstantLocal;
                if (wasConstant && Mathf.Approximately(targetX, newX) && Mathf.Approximately(targetY, newY)) {
                    comp.Mode = LookTargetMode.Disabled;
                }
            }
        }
    }
}