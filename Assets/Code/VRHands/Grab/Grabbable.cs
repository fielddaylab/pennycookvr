using System;
using System.Collections;
using System.Collections.Generic;
using BeauUtil;
using BeauRoutine;
using FieldDay.Components;
using FieldDay.Sockets;
using FieldDay.HID.XR;
using ScriptableBake;
using UnityEngine;


namespace FieldDay.VRHands {
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public class Grabbable : BatchedComponent, IBaked, IRegistrationCallbacks {
        #region Inspector

        public bool GrabEnabled = true;

        [Header("Configuration")]
        [Range(1, 2)] public int MaxGrabbers = 2;
        public bool IsHeavy = false;
        public bool MustGrabAtSnap = false;
        public GrabbablePoseAnim GrabberAnim;

        [Header("Snap Node Data -- DO NOT EDIT")]
        [HideInInspector] public GrabbableSnapNodeData[] SnapNodes;
        [HideInInspector] public OffsetLengthU16 BothSnapNodeRange;
        [HideInInspector] public OffsetLengthU16 LeftSnapNodeRange;
        [HideInInspector] public OffsetLengthU16 RightSnapNodeRange;
        [HideInInspector] public OffsetLengthU16 DynamicBothSnapNodeRange;
        [HideInInspector] public OffsetLengthU16 DynamicLeftSnapNodeRange;
        [HideInInspector] public OffsetLengthU16 DynamicRightSnapNodeRange;

        #endregion // Inspector

        [NonSerialized] public Transform CachedTransform;
        [NonSerialized] public Rigidbody CachedRB;
        [NonSerialized] public bool DefaultRBKinematic;

        [NonSerialized] public Grabber[] CurrentGrabbers;
        [NonSerialized] public int CurrentGrabberCount;

        [NonSerialized] public BitSet32 DisabledSnapNodes;
        [NonSerialized] public BitSet32 UsedSnapNodes;

        #region Events

        public readonly CastableEvent<Grabber> OnGrabbed = new CastableEvent<Grabber>();
        public readonly CastableEvent<Grabber> OnGrabUpdate = new CastableEvent<Grabber>();
        public readonly CastableEvent<Grabber> OnReleased = new CastableEvent<Grabber>();

        #endregion // Events


        private void Awake() {
            this.CacheComponent(ref CachedTransform);
            this.CacheComponent(ref CachedRB);

            DefaultRBKinematic = CachedRB.isKinematic;
            CurrentGrabbers = new Grabber[MaxGrabbers];
        }

        void IRegistrationCallbacks.OnRegister() {
            if (MaxGrabbers > 1 && SnapNodes.Length > 1) {
                CachedRB.solverIterations = 10;
                CachedRB.solverVelocityIterations = 4;
            }
        }

        void IRegistrationCallbacks.OnDeregister() {
        }

        #region IBaked

#if UNITY_EDITOR

        int IBaked.Order => 0;

        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
            GrabbableSnapNode[] snapNodes = GetComponentsInChildren<GrabbableSnapNode>(true);
            GrabbableSnapNodeData[] dataNodes = new GrabbableSnapNodeData[snapNodes.Length];

            List<GrabbableSnapNodeData> staticLocations = new List<GrabbableSnapNodeData>();
            List<GrabbableSnapNodeData> dynamicLocations = new List<GrabbableSnapNodeData>();

            // gather locations

            foreach(var node in snapNodes) {
                if (!node.isActiveAndEnabled) {
                    continue;
                }

                GrabbableSnapNodeData data = default;
                data.Name = node.gameObject.name;

                switch (node.ValidHandType) {
                    case XRHandIndex.Any: {
                        data.Flags |= GrabbableSnapFlags.AllowLeftHand | GrabbableSnapFlags.AllowRightHand;
                        break;
                    }
                    case XRHandIndex.Left: {
                        data.Flags |= GrabbableSnapFlags.AllowLeftHand;
                        break;
                    }
                    case XRHandIndex.Right: {
                        data.Flags |= GrabbableSnapFlags.AllowRightHand;
                        break;
                    }
                }

                if (node.IsDynamic) {
                    data.Flags |= GrabbableSnapFlags.IsDynamic;
                    data.DynamicPose = node.transform;
                    dynamicLocations.Add(data);
                } else {
                    Pose nodePose;
                    node.transform.GetPositionAndRotation(out nodePose.position, out nodePose.rotation);

                    Vector3 rotForward = Geom.Forward(nodePose.rotation);
                    Vector3 rotUp = Geom.Up(nodePose.rotation);

                    nodePose.position = transform.InverseTransformPoint(nodePose.position);
                    nodePose.rotation = Quaternion.LookRotation(transform.InverseTransformDirection(rotForward), transform.InverseTransformDirection(rotUp));

                    data.RelativePose = nodePose;
                    staticLocations.Add(data);
                }
            }

            // start adding to lists
            int writeHead = 0;
            AppendToNodeList(dataNodes, ref writeHead, ref BothSnapNodeRange, staticLocations, GrabbableSnapFlags.BothHands);
            AppendToNodeList(dataNodes, ref writeHead, ref LeftSnapNodeRange, staticLocations, GrabbableSnapFlags.AllowLeftHand);
            AppendToNodeList(dataNodes, ref writeHead, ref RightSnapNodeRange, staticLocations, GrabbableSnapFlags.AllowRightHand);

            AppendToNodeList(dataNodes, ref writeHead, ref DynamicBothSnapNodeRange, dynamicLocations, GrabbableSnapFlags.BothHands);
            AppendToNodeList(dataNodes, ref writeHead, ref DynamicLeftSnapNodeRange, dynamicLocations, GrabbableSnapFlags.AllowLeftHand);
            AppendToNodeList(dataNodes, ref writeHead, ref DynamicRightSnapNodeRange, dynamicLocations, GrabbableSnapFlags.AllowRightHand);

            SnapNodes = dataNodes;

            // clean up editor nodes

            for (int i = snapNodes.Length - 1; i >= 0; i--) {
                Transform t = snapNodes[i].transform;
                bool preserve = snapNodes[i].IsDynamic;
                Baking.Destroy(snapNodes[i]);
                if (!preserve && Baking.IsEmptyLeaf(t)) {
                    Baking.Destroy(t.gameObject);
                }
            }

            return true;
        }

        static private void AppendToNodeList(GrabbableSnapNodeData[] output, ref int writeTarget, ref OffsetLengthU16 outputRange, List<GrabbableSnapNodeData> nodeList, GrabbableSnapFlags desiredFlags) {
            int offset = writeTarget;
            int length = 0;

            for(int i = nodeList.Count - 1; i >= 0; i--) {
                GrabbableSnapNodeData data = nodeList[i];
                if ((data.Flags & desiredFlags) == desiredFlags) {
                    output[writeTarget++] = data;
                    length++;
                    nodeList.FastRemoveAt(i);
                }
            }

            if (length > 0) {
                using (var compare = ArrayUtils.WrapComparison<GrabbableSnapNodeData>((a, b) => a.Name.CompareTo(b.Name))) {
                    Array.Sort(output, offset, length, compare);
                }
            }

            outputRange.Offset = (ushort) offset;
            outputRange.Length = (ushort) length;
        }

#endif // UNITY_EDITOR

        #endregion // IBaked
    }

    [Serializable]
    public struct GrabbableSnapNodeData {
        public Pose RelativePose;
        [AutoEnum] public GrabbableSnapFlags Flags;
        public Transform DynamicPose;
        public StringHash32 Name;
    }

    [Serializable]
    public struct GrabbablePoseAnim {
        public int GripAnimIndex;
        public float GripAnimStrength;
    }

    [Flags]
    public enum GrabbableSnapFlags : uint {
        AllowLeftHand = 0x01,
        AllowRightHand = 0x02,
        IsDynamic = 0x04,

        [Hidden] BothHands = AllowLeftHand | AllowRightHand,
    }
}