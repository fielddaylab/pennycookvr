using System;
using BeauUtil;
using FieldDay.Components;
using ScriptableBake;
using UnityEngine;

namespace Pennycook {
    public sealed class SyncedRig : BatchedComponent, IBaked {
        [Flags]
        public enum Settings : uint {
            CheckVisibility = 0x01,
            CheckAnimating = 0x02,
            RootIsInLocalSpace = 0x04,
            SkipRootUpdate = 0x08
        }

        [Serializable]
        public struct Bone {
            [Required] public Transform Source;
            [Required] public Transform Copy;
        }

        public Settings Options;
        [Space]
        public Renderer Skin;
        public Animator Animator;
        [Space]
        public Bone Root;
        public Bone[] Bones;


#if UNITY_EDITOR
        int IBaked.Order { get { return 0; } }

        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
            Sync(this);
            return true;
        }

        [ContextMenu("Sync")]
        private void ContextMenu_Sync() {
            Sync(this);
        }

#endif // UNITY_EDITOR

        static public void Sync(SyncedRig rig) {
            Vector3 pos;
            Quaternion rot;
            SyncedRig.Bone bone;

            Bits.Set(ref rig.Options, SyncedRig.Settings.RootIsInLocalSpace, rig.Root.Source.parent == rig.Root.Copy.parent);

            bone = rig.Root;
            if ((rig.Options & SyncedRig.Settings.RootIsInLocalSpace) != 0) {
                bone.Source.GetLocalPositionAndRotation(out pos, out rot);
                bone.Copy.SetLocalPositionAndRotation(pos, rot);
            } else {
                bone.Source.GetPositionAndRotation(out pos, out rot);
                bone.Copy.SetPositionAndRotation(pos, rot);
            }

            var boneList = rig.Bones;
            for(int i = 0, len = boneList.Length; i < len; i++) {
                bone = boneList[i];
                bone.Source.GetPositionAndRotation(out pos, out rot);
                bone.Copy.SetPositionAndRotation(pos, rot);
            }
        }
    }
}