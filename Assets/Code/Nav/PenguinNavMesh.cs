using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Graph;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.Scenes;
using FieldDay.SharedState;
using FieldDay.Threading;
using ScriptableBake;
using UnityEngine;

namespace Pennycook {
    [SharedStateInitOrder(5)]
    public sealed class PenguinNavMesh : SharedStateComponent, IScenePreload, IBaked {
        [HideInInspector] public NavPost[] Posts;
        public float BucketResolution = 2;

        [NonSerialized] public NodeGraph Graph;
        [NonSerialized] public NavRegionGrid HashParams;
        [NonSerialized] public UnsafeSpan<NavMeshNodeBucket> NodeSpatialHash;
        [NonSerialized] public AtomicRWLock GraphLock;

        public IEnumerator<WorkSlicer.Result?> Preload() {
            foreach(var post in Posts) {
                post.Id = post.name;
                post.Position = post.transform.position;
                yield return null;
            }

            var grid = PenguinNav.WalkGrid;
            while(grid.LoadHandle.IsRunning()) {
                yield return WorkSlicer.Result.HaltForFrame;
            }

            HashParams = PenguinNav.WalkGrid.GridParams.Reslice(BucketResolution);
            NodeSpatialHash = NavMemory.CreateGrid<NavMeshNodeBucket>(HashParams.CountX, HashParams.CountZ);

            Game.Scenes.RegisterLoadDependency(Async.Schedule(PenguinNavMeshGenerator.ThreadedGenerateMesh, AsyncFlags.HighPriority));
        }

        #region Debugging

        private void LateUpdate() {
            //if (Graph != null) {
            //    RenderDebug();
            //}
        }

        private void RenderDebug() {
            if (Atomics.CanAcquireRead(ref GraphLock)) {
                Atomics.AcquireRead(ref GraphLock);

                for(ushort nodeIdx = 0; nodeIdx < Graph.NodeCount(); nodeIdx++) {
                    var node = Graph.Node(nodeIdx);
                    DebugDraw.AddPoint(node.Position, 0.15f, Color.green);
                }

                for(ushort edgeIdx = 0; edgeIdx < Graph.EdgeCount(); edgeIdx++) {
                    var edge = Graph.Edge(edgeIdx);
                    var start = Graph.Node(edge.StartIndex);
                    var end = Graph.Node(edge.EndIndex);

                    DebugDraw.AddLine(start.Position, end.Position, ColorBank.ForestGreen, 0.12f, 0, false);
                }

                Atomics.ReleaseRead(ref GraphLock);
            }
        }

        #endregion // Debugging

#if UNITY_EDITOR

        int IBaked.Order { get { return 10; } }

        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
            Posts = GetComponentsInChildren<NavPost>();

            List<NavPost> finalNeighbors = new List<NavPost>();
            foreach(var post in Posts) {
                foreach(var neighbor in Posts) {
                    if (post == neighbor) {
                        continue;
                    }

                    float distSq = Vector3.SqrMagnitude(post.transform.position - neighbor.transform.position);
                    float rad = post.Radius + neighbor.Radius + 0.1f;
                    if (distSq <= rad * rad) {
                        finalNeighbors.Add(neighbor);
                    }
                }
                post.Neighbors = finalNeighbors.ToArray();
                finalNeighbors.Clear();
            }

            return true;
        }

#endif // UNITY_EDITOR
    }

    public unsafe struct NavMeshNodeBucket {
        public const int Capacity = 15;

        public fixed ushort NodeIds[Capacity];
        public byte NodeCount;
    }

    static internal class PenguinNavMeshGenerator {
        static internal void ThreadedGenerateMesh() {
            var mesh = PenguinNav.Mesh;

            using (Profiling.Time("generating nav graph", ProfileTimeUnits.Microseconds)) {
                Atomics.AcquireWrite(ref mesh.GraphLock);

                mesh.Graph = new NodeGraph();

                foreach (var post in mesh.Posts) {
                    GenerateNodesForPost(mesh.HashParams, mesh.NodeSpatialHash, post, mesh.Graph);
                }

                foreach (var post in mesh.Posts) {
                    GenerateConnectionsForPost(post, mesh.Graph);
                }

                mesh.Graph.OptimizeEdgeOrder();

                Atomics.ReleaseWrite(ref mesh.GraphLock);
            }
        }

        private const int RingPoints = 6;
        private const float NeighborConnectionDotProductThreshold = 0.65f;
        private const float RingDistanceScale = 0.85f;
        private const float RingGenerationRadiusThreshold = 1.25f;

        static private unsafe void GenerateNodesForPost(in NavRegionGrid grid, UnsafeSpan<NavMeshNodeBucket> buckets, NavPost post, NodeGraph graph) {
            Vector3 postPos = post.Position;
            float postRad = post.Radius;

            ushort startingNode = NodeGraph.InvalidId;
            ushort nodeCount = 0;

            ushort middle;
            if (PenguinNav.IsWalkable(ref postPos)) {
                middle = graph.AddNode(post.Id, 0, postPos, post);
                InsertNodeIntoSpatialHash(grid, buckets, postPos, middle);
                startingNode = middle;
                nodeCount++;
            } else {
                middle = NodeGraph.InvalidId;
            }

            if (postRad > RingGenerationRadiusThreshold) {

                ushort* prevNodeBuffer = stackalloc ushort[RingPoints];
                ushort prevNode;

                for (int i = 0; i < RingPoints; i++) {
                    prevNodeBuffer[i] = NodeGraph.InvalidId;
                }

                float dist = postRad * RingDistanceScale;
                float arcDist = dist * Mathf.PI * 2 / RingPoints;
                for (int arc = 0; arc < RingPoints; arc++) {
                    Vector3 newPos = postPos + Geom.SwizzleYZ(Geom.Normalized(Mathf.PI * 2 * arc / RingPoints, dist));
                    if (PenguinNav.IsWalkable(ref newPos)) {
                        ushort newNode = graph.AddNode(post.Id, 0, newPos, post);
                        InsertNodeIntoSpatialHash(grid, buckets, newPos, newNode);
                        if (startingNode == NodeGraph.InvalidId) {
                            startingNode = newNode;
                        }
                        nodeCount++;
                        if (middle != NodeGraph.InvalidId && PenguinNav.IsWalkableRaycast(postPos, newPos)) {
                            graph.AddEdge(middle, newNode, dist);
                            graph.AddEdge(newNode, middle, dist);
                        }

                        if (arc > 0 && (prevNode = prevNodeBuffer[arc - 1]) != NodeGraph.InvalidId) {
                            graph.AddEdge(newNode, prevNode, arcDist);
                            graph.AddEdge(prevNode, newNode, arcDist);
                        }

                        prevNodeBuffer[arc] = newNode;
                    }
                }

                if (prevNodeBuffer[0] != NodeGraph.InvalidId && prevNodeBuffer[RingPoints - 1] != NodeGraph.InvalidId) {
                    graph.AddEdge(prevNodeBuffer[0], prevNodeBuffer[RingPoints - 1], arcDist);
                    graph.AddEdge(prevNodeBuffer[RingPoints - 1], prevNodeBuffer[0], arcDist);
                }
            }

            post.Nodes = new OffsetLengthU16(startingNode, nodeCount);
        }

        static private unsafe void GenerateConnectionsForPost(NavPost post, NodeGraph graph) {
            OffsetLengthU16 startNodes = post.Nodes;
            foreach (var neighbor in post.Neighbors) {
                OffsetLengthU16 endNodes = neighbor.Nodes;

                Vector3 towardsNeighbor = Vector3.Normalize(neighbor.Position - post.Position);
                Vector3 towardsPost = -towardsNeighbor;

                for (ushort start = startNodes.Offset; start < startNodes.End; start++) {
                    Vector3 startPos = graph.Node(start).Position;

                    bool startIsCenter = PenguinNav.SqrDistFlat(startPos - post.Position) <= 0.05f;

                    if (startIsCenter && startNodes.Length > 2) {
                        continue;
                    }

                    if (!startIsCenter && Vector3.Dot(Vector3.Normalize(startPos - post.Position), towardsNeighbor) < NeighborConnectionDotProductThreshold) {
                        continue;
                    }

                    for (ushort end = endNodes.Offset; end < endNodes.End; end++) {
                        Vector3 endPos = graph.Node(end).Position;

                        bool endIsCenter = PenguinNav.SqrDistFlat(endPos - neighbor.Position) <= 0.05f;

                        if (!startIsCenter && endIsCenter && endNodes.Length > 2) {
                            continue;
                        }

                        if (!endIsCenter && Vector3.Dot(Vector3.Normalize(endPos - neighbor.Position), towardsPost) < NeighborConnectionDotProductThreshold) {
                            continue;
                        }

                        if (PenguinNav.IsWalkableRaycast(startPos, endPos)) {
                            graph.AddEdge(start, end, Vector3.Distance(startPos, endPos));
                        }
                    }
                }
            }
        }

        static private unsafe void InsertNodeIntoSpatialHash(in NavRegionGrid grid, UnsafeSpan<NavMeshNodeBucket> buckets, Vector3 position, ushort nodeId) {
            if (grid.TryGetVoxel(position, out int v)) {
                ref NavMeshNodeBucket bucket = ref buckets[v];
                Assert.True(bucket.NodeCount < NavMeshNodeBucket.Capacity, "Too many nodes for bucket {0} ({1})", v, position);
                bucket.NodeIds[bucket.NodeCount++] = nodeId;
            }
        }

    }

    static public partial class PenguinNav {
        [SharedStateReference]
        static public PenguinNavMesh Mesh { get; private set; }

        static public unsafe bool TryFindClosestWalkableNodeToPoint(Vector3 position, out ushort nodeId) {
            if (IsWalkable(position) && Mesh.HashParams.TryGetVoxel(position, out int bucketIdx)) {
                NavMeshNodeBucket bucket = Mesh.NodeSpatialHash[bucketIdx];

                float minDist = float.MaxValue;
                ushort minNodeId = NodeGraph.InvalidId;
                for(int i = 0; i < bucket.NodeCount; i++) {
                    ushort checkedNodeId = bucket.NodeIds[i];

                    Vector3 nodePos = Mesh.Graph.Node(checkedNodeId).Position;
                    float dist = SqrDistFlat(nodePos, position);
                    if (dist < minDist && IsWalkableRaycast(position, nodePos)) {
                        minDist = dist;
                        minNodeId = checkedNodeId;
                    }
                }

                nodeId = minNodeId;
                return nodeId != NodeGraph.InvalidId;
            }

            nodeId = NodeGraph.InvalidId;
            return false;
        }

        #region Math

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal float SqrDistFlat(Vector3 a, Vector3 b) {
            Vector3 d = b - a;
            return (d.x * d.x) + (d.z * d.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal float SqrDistFlat(Vector3 d) {
            return (d.x * d.x) + (d.z * d.z);
        }

        #endregion // Math
    }
}