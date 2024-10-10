using BeauUtil;
using FieldDay;
using FieldDay.Audio;
using FieldDay.Systems;
using Pennycook.Environment;
using UnityEngine;

namespace Pennycook.Animation {
    [SysUpdate(GameLoopPhase.LateUpdate)]
    public sealed class FootstepEffectSystem : ComponentSystemBehaviour<FootstepPlayer> {
        public override void ProcessWorkForComponent(FootstepPlayer component, float deltaTime) {
            if (component.Cull) {
                component.IsQueued = false;
                return;
            }

            if (!component.IsQueued) {
                return;
            }

            component.IsQueued = false;

            bool isLeft = component.LastFoot == FootstepIndex.Left || component.LastFoot == FootstepIndex.Both;
            bool isRight = component.LastFoot == FootstepIndex.Right || component.LastFoot == FootstepIndex.Both;

            StringHash32 stepSfx;
            switch (component.FootstepType) {
                case FootstepType.Default:
                default:
                    stepSfx = component.DefaultStep;
                    break;

                case FootstepType.Soft:
                    stepSfx = component.SoftStep;
                    break;

                case FootstepType.Lift:
                    stepSfx = component.LiftStep;
                    break;
            }

            if (isLeft) {
                Sfx.PlayDetached(stepSfx, component.LeftFoot);
                VFXUtility.QueueFootstepDecal(component.LeftFoot, component.DecalType | FootstepDecalType.LeftFoot);
                // TODO: decals/particles
            }

            if (isRight) {
                Sfx.PlayDetached(stepSfx, component.RightFoot);
                VFXUtility.QueueFootstepDecal(component.RightFoot, component.DecalType | FootstepDecalType.RightFoot);
                // TODO: decals/particles
            }
        }
    }
}