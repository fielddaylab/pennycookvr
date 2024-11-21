using System;
using BeauUtil;
using FieldDay;
using FieldDay.Components;
using FieldDay.Systems;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;

namespace Pennycook {
    [SysUpdate(GameLoopPhaseMask.LateFixedUpdate | GameLoopPhaseMask.LateUpdate, -1000)]
    public sealed class SyncedRigSystem : ComponentSystemBehaviour<SyncedRig> {
        public override void ProcessWork(float deltaTime) {
            using(new PhysicsAutoSyncScope(false)) {
                for(int i = 0, len = m_Components.Count; i < len; i++) {
                    ProcessWorkForComponent(m_Components[i], deltaTime);
                }
            }
        }

        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [Il2CppSetOption(Option.NullChecks, false)]
        public override void ProcessWorkForComponent(SyncedRig component, float deltaTime) {
            if ((component.Options & SyncedRig.Settings.CheckAnimating) != 0 && (!component.Animator || !component.Animator.isActiveAndEnabled)) {
                return;
            }
            if ((component.Options & SyncedRig.Settings.CheckVisibility) != 0 && (!component.Skin || !component.Skin.isVisible)) {
                return;
            }

            Vector3 pos;
            Quaternion rot;
            SyncedRig.Bone bone;

            if ((component.Options & SyncedRig.Settings.SkipRootUpdate) == 0) {
                bone = component.Root;
                if ((component.Options & SyncedRig.Settings.RootIsInLocalSpace) != 0) {
                    bone.Source.GetLocalPositionAndRotation(out pos, out rot);
                    bone.Copy.SetLocalPositionAndRotation(pos, rot);
                } else {
                    bone.Source.GetPositionAndRotation(out pos, out rot);
                    bone.Copy.SetPositionAndRotation(pos, rot);
                }
            }

            var boneList = component.Bones;
            for(int i = 0, len = boneList.Length; i < len; i++) {
                bone = boneList[i];
                bone.Source.GetPositionAndRotation(out pos, out rot);
                bone.Copy.SetPositionAndRotation(pos, rot);
            }
        }

        protected override void OnComponentAdded(SyncedRig component) {
            SyncedRig.Sync(component);
        }
    }
}