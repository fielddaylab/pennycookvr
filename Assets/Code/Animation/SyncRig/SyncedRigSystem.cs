//#define JOBIFIED
 
using System;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Components;
using FieldDay.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.IL2CPP.CompilerServices;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

namespace Pennycook {
    [SysUpdate(GameLoopPhaseMask.Update | GameLoopPhaseMask.LateUpdate, -1000)]
    public sealed class SyncedRigSystem : ComponentSystemBehaviour<SyncedRig> {
        private const int MaxTransforms = 256;

        public override void ProcessWork(float deltaTime) {
            using (Profiling.Sample("SyncedRigSystem")) {
#if JOBIFIED
                ProcessJobified();
#else
                using(new PhysicsAutoSyncScope(false)) {
                    for(int i = 0, len = m_Components.Count; i < len; i++) {
                        ProcessWorkForComponent(m_Components[i], deltaTime);
                    }
                }
#endif // JOBIFIED
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

#if JOBIFIED

        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [Il2CppSetOption(Option.NullChecks, false)]
        private void ProcessJobified() {
            NativeArray<Vector3> positions = new NativeArray<Vector3>(MaxTransforms, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            NativeArray<Quaternion> rotations = new NativeArray<Quaternion>(MaxTransforms, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            TransformAccessArray inputTransforms = new TransformAccessArray(MaxTransforms);
            TransformAccessArray outputTransforms = new TransformAccessArray(MaxTransforms);

            for (int i = 0, len = m_Components.Count; i < len; i++) {
                GatherInputs(m_Components[i], ref inputTransforms, ref outputTransforms);
            }

            GatherJob gatherJob;
            gatherJob.Positions = positions;
            gatherJob.Rotations = rotations;

            JobHandle gatherHandle = gatherJob.ScheduleReadOnly(inputTransforms, 32);

            WriteJob writeJob;
            writeJob.Positions = positions;
            writeJob.Rotations = rotations;

            JobHandle writeHandle = writeJob.Schedule(outputTransforms, gatherHandle);

            JobHandle.ScheduleBatchedJobs();
            writeHandle.Complete();

            inputTransforms.Dispose();
            positions.Dispose();
            rotations.Dispose();
        }

        static private void GatherInputs(SyncedRig component, ref TransformAccessArray inputs, ref TransformAccessArray outputs) {
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
            for (int i = 0, len = boneList.Length; i < len; i++) {
                bone = boneList[i];
                inputs.Add(bone.Source.GetInstanceID());
                outputs.Add(bone.Copy.GetInstanceID());
            }
        }

        

        [BurstCompile]
        private struct GatherJob : IJobParallelForTransform {
            public NativeArray<Vector3> Positions;
            public NativeArray<Quaternion> Rotations;

            public void Execute(int index, TransformAccess transform) {
                if (transform.isValid) {
                    transform.GetPositionAndRotation(out Vector3 pos, out Quaternion rot);
                    Positions[index] = pos;
                    Rotations[index] = rot;
                }
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelForTransform {
            [ReadOnly]
            public NativeArray<Vector3> Positions;

            [ReadOnly]
            public NativeArray<Quaternion> Rotations;

            public void Execute(int index, TransformAccess transform) {
                if (transform.isValid) {
                    transform.SetPositionAndRotation(Positions[index], Rotations[index]);
                }
            }
        }

#endif // JOBIFIED
    }
}