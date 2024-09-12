using System;
using System.Collections.Generic;
using System.Threading;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Graph;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.Scenes;
using FieldDay.SharedState;
using FieldDay.Systems;
using FieldDay.Threading;
using ScriptableBake;
using UnityEngine;

namespace Pennycook {
    [SharedStateInitOrder(5)]
    public sealed class PenguinNavMesh : SharedStateComponent, IScenePreload, IBaked {
        public NavPost[] Posts;

        [NonSerialized] public NodeGraph Graph;
        [NonSerialized] public AtomicLock WriteLock;

        public IEnumerator<WorkSlicer.Result?> Preload() {
            foreach(var post in Posts) {

            }

            var grid = PenguinUtility.WalkGrid;
            while(grid.LoadHandle.IsRunning()) {
                yield return WorkSlicer.Result.HaltForFrame;
            }

            Game.Scenes.RegisterLoadDependency(Async.Schedule(ThreadedGenerateGraph, AsyncFlags.HighPriority));
        }

        private unsafe void ThreadedGenerateGraph() {
            using (Profiling.Time("generating nav graph", ProfileTimeUnits.Microseconds)) {
                Atomics.AcquireWrite(ref WriteLock);

                Graph = new NodeGraph();

                foreach (var post in Posts) {
                    GenerateNodesForPost(post);
                }

                foreach (var post in Posts) {
                    GenerateConnectionsForPost(post);
                }

                Graph.OptimizeEdgeOrder();

                Atomics.ReleaseWrite(ref WriteLock);
            }
        }

        private const int RingPoints = 6;
        private const float NeighborConnectionDotProductThreshold = 0.75f;

        private unsafe void GenerateNodesForPost(NavPost post) {
            Vector3 postPos = post.Position;
            float postRad = post.Radius;

            ushort startingNode = NodeGraph.InvalidId;
            ushort nodeCount = 0;

            ushort middle;
            if (PenguinUtility.IsWalkable(ref postPos)) {
                middle = Graph.AddNode(default, 0, postPos);
                startingNode = middle;
                nodeCount++;
            } else {
                middle = NodeGraph.InvalidId;
            }

            if (postRad > 0.2f) {

                ushort* prevNodeBuffer = stackalloc ushort[RingPoints];
                ushort prevNode;

                for (int i = 0; i < RingPoints; i++) {
                    prevNodeBuffer[i] = NodeGraph.InvalidId;
                }

                float dist = postRad;
                float arcDist = dist * Mathf.PI * 2 / RingPoints;
                for (int arc = 0; arc < RingPoints; arc++) {
                    Vector3 newPos = postPos + Geom.SwizzleYZ(Geom.Normalized(Mathf.PI * 2 * arc / RingPoints, dist));
                    if (PenguinUtility.IsWalkable(ref newPos)) {
                        ushort newNode = Graph.AddNode(default, 0, newPos);
                        if (startingNode == NodeGraph.InvalidId) {
                            startingNode = newNode;
                        }
                        nodeCount++;
                        if (middle != NodeGraph.InvalidId) {
                            Graph.AddEdge(middle, newNode, dist);
                            Graph.AddEdge(newNode, middle, dist);
                        }

                        if (arc > 0 && (prevNode = prevNodeBuffer[arc - 1]) != NodeGraph.InvalidId) {
                            Graph.AddEdge(newNode, prevNode, arcDist);
                            Graph.AddEdge(prevNode, newNode, arcDist);
                        }

                        prevNodeBuffer[arc] = newNode;
                    }
                }

                if (prevNodeBuffer[0] != NodeGraph.InvalidId && prevNodeBuffer[RingPoints - 1] != NodeGraph.InvalidId) {
                    Graph.AddEdge(prevNodeBuffer[0], prevNodeBuffer[RingPoints - 1], arcDist);
                    Graph.AddEdge(prevNodeBuffer[RingPoints - 1], prevNodeBuffer[0], arcDist);
                }
            }

            post.Nodes = new OffsetLengthU16(startingNode, nodeCount);
        }

        private unsafe void GenerateConnectionsForPost(NavPost post) {
            OffsetLengthU16 startNodes = post.Nodes;
            foreach(var neighbor in post.Neighbors) {
                OffsetLengthU16 endNodes = neighbor.Nodes;

                Vector3 towardsNeighbor = Vector3.Normalize(neighbor.Position - post.Position);
                Vector3 towardsPost = -towardsNeighbor;

                for (ushort start = startNodes.Offset; start < startNodes.End; start++) {
                    Vector3 startPos = Graph.Node(start).Position;

                    if (Vector3.SqrMagnitude(startPos - post.Position) > 0.05f
                        && Vector3.Dot(Vector3.Normalize(startPos - post.Position), towardsNeighbor) < NeighborConnectionDotProductThreshold) {
                        continue;
                    }

                    for(ushort end = endNodes.Offset; end < endNodes.End; end++) {
                        Vector3 endPos = Graph.Node(end).Position;

                        if (Vector3.SqrMagnitude(endPos - neighbor.Position) > 0.05f
                            && Vector3.Dot(Vector3.Normalize(endPos - neighbor.Position), towardsPost) < NeighborConnectionDotProductThreshold) {
                            continue;
                        }

                        if (PenguinUtility.IsWalkableRaycast(startPos, endPos)) {
                            Graph.AddEdge(start, end, Vector3.Distance(startPos, endPos));
                        }
                    }
                }
            }
        }

        #region Debugging

        private void LateUpdate() {
            if (Graph != null) {
                RenderDebug();
            }
        }

        private void RenderDebug() {
            if (Atomics.CanAcquireRead(ref WriteLock)) {
                Atomics.AcquireRead(ref WriteLock);

                for(ushort nodeIdx = 0; nodeIdx < Graph.NodeCount(); nodeIdx++) {
                    var node = Graph.Node(nodeIdx);
                    DebugDraw.AddPoint(node.Position, 0.1f, Color.green);
                }

                for(ushort edgeIdx = 0; edgeIdx < Graph.EdgeCount(); edgeIdx++) {
                    var edge = Graph.Edge(edgeIdx);
                    var start = Graph.Node(edge.StartIndex);
                    var end = Graph.Node(edge.EndIndex);

                    DebugDraw.AddLine(start.Position, end.Position, ColorBank.ForestGreen, 0.08f);
                }

                Atomics.ReleaseRead(ref WriteLock);
            } else {
                Log.Msg("Still writing!");
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

    static public partial class PenguinUtility {
        [SharedStateReference]
        static public PenguinNavMesh NavMesh { get; private set; }

        //static public bool TryFindPath(Vector3 start, Vector3 end, ref NodePath path) {
        //    return Pathfinder.AStar(NavMesh, ref path, )
        //}
    }
}