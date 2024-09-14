using System;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Graph;
using FieldDay.Systems;
using UnityEngine;

namespace Pennycook {
    public sealed class PenguinPathfinderRequestSystem : SharedStateSystemBehaviour<PenguinPathfinderState> {
        static private readonly Action ProcessRequestsThreaded_Delegate = ProcessRequestsThreaded;

        public override void ProcessWork(float deltaTime) {
            if (m_State.RequestHandler.IsRunning()) {
                return;
            }

            m_State.BufferLock.AcquireRead();
            bool hasElements = m_State.RequestBuffer.Count > 0;
            m_State.BufferLock.ReleaseRead();

            if (hasElements) {
                m_State.RequestHandler = Async.Schedule(ProcessRequestsThreaded_Delegate, AsyncFlags.Default);
            }
        }

        static private void ProcessRequestsThreaded() {
            var state = PenguinUtility.Pathfinder;

            while (true) {
                state.BufferLock.AcquireWrite();
                bool poppedRequest = state.RequestBuffer.TryPopFront(out var request);
                state.BufferLock.ReleaseWrite();

                if (!poppedRequest) {
                    return;
                }

                ProcessRequest(state, request);
            }
        }

        static private void ProcessRequest(PenguinPathfinderState state, PenguinPathRequest request) {
            state.RequestIdLock.AcquireRead();
            bool requestIsValid = state.RequestIdGenerator.IsValid(request.Id);
            state.RequestIdLock.ReleaseRead();

            if (!requestIsValid) {
                return;
            }

            Vector3 start = request.Start;
            Vector3 end = request.End;

            NavPath outputPath;
            state.PoolLock.AcquireWrite();
            outputPath = state.PathPool.Alloc();
            state.PoolLock.ReleaseWrite();

            outputPath.Positions.Clear();

            bool validPath;

            if (PenguinUtility.IsWalkableRaycast(start, end)) {
                outputPath.Positions.PushBack(end);
                validPath = true;
            } else {
                NodePath p = state.WorkPath;
                p.Reset();

                // TODO: find closest node to start and end
                ushort closestStart, closestEnd;

                bool found = Pathfinder.AStar(PenguinUtility.NavMesh.Graph, ref p, closestStart, closestEnd);

                if (found) {
                    validPath = true;
                    
                    int traversalCount = p.Length();
                    Vector3 simplificationStart = start;
                    int simplificationAccum = 0;
                    for(int i = 0; i < traversalCount; i++) {
                        var traversal = p.Traversal(i);
                        Vector3 nextPos = PenguinUtility.NavMesh.Graph.Node(traversal.NodeId).Position;
                        if (simplificationAccum > 0) {
                            if (PenguinUtility.IsWalkableRaycast(simplificationStart, nextPos)) {
                                outputPath.Positions.PopBack();
                                simplificationAccum++;
                            } else {
                                simplificationStart = nextPos;
                                simplificationAccum = 0;
                            }
                        } else {
                            simplificationAccum++;
                        }
                        outputPath.Positions.PushBack(nextPos);
                    }
                    outputPath.Positions.PushBack(end);
                } else {
                    validPath = false;
                }
            }

            state.RequestIdLock.AcquireWrite();
            bool isRequestStillValid = state.RequestIdGenerator.IsValid(request.Id);
            if (isRequestStillValid) {
                state.RequestIdGenerator.Free(request.Id);
            }
            state.RequestIdLock.ReleaseWrite();

            if (!validPath || !isRequestStillValid) {
                state.PoolLock.AcquireWrite();
                outputPath.Positions.Clear();
                state.PathPool.Free(outputPath);
                state.PoolLock.ReleaseWrite();
                outputPath = null;
            }

            if (isRequestStillValid) {
                Async.InvokeAsync(() => request.OnPathResolved(outputPath));
            }
        }
    }
}