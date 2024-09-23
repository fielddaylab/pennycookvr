using System;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Graph;
using FieldDay;
using FieldDay.Systems;
using UnityEngine;

namespace Pennycook {
    [SysUpdate(GameLoopPhaseMask.UnscaledLateUpdate | GameLoopPhaseMask.UnscaledUpdate | GameLoopPhaseMask.PreUpdate)]
    public sealed class PenguinPathRequestSystem : SharedStateSystemBehaviour<PenguinPathRequestState> {
        static private readonly Action ProcessRequestsThreaded_Delegate = ProcessRequestsThreaded;

        public override void ProcessWork(float deltaTime) {
            while(true) {
                m_State.ResponseLock.AcquireWrite();
                bool hasResponse = m_State.ResponseBuffer.TryPopFront(out var response);
                m_State.ResponseLock.ReleaseWrite();
                if (!hasResponse) {
                    break;
                }

                response.Handler(response.Path, response.Context);
            }

            if (m_State.RequestHandler.IsRunning()) {
                return;
            }

            m_State.RequestLock.AcquireRead();
            bool hasElements = m_State.RequestBuffer.Count > 0;
            m_State.RequestLock.ReleaseRead();

            if (hasElements) {
                m_State.RequestHandler = Async.Schedule(ProcessRequestsThreaded_Delegate, AsyncFlags.Default);
            }
        }

        static private void ProcessRequestsThreaded() {
            var state = PenguinNav.Requests;

            while (true) {
                state.RequestLock.AcquireWrite();
                bool poppedRequest = state.RequestBuffer.TryPopFront(out var request);
                state.RequestLock.ReleaseWrite();

                if (!poppedRequest) {
                    return;
                }

                ProcessRequest(state, request);
            }
        }

        static private void ProcessRequest(PenguinPathRequestState state, PenguinPathRequest request) {
            state.RequestIdLock.AcquireRead();
            bool requestIsValid = state.RequestIdGenerator.IsValid(request.Id);
            state.RequestIdLock.ReleaseRead();

            if (!requestIsValid) {
                return;
            }

            Vector3 start = PenguinNav.SnapPositionToApproximateGround(request.Start);
            Vector3 end = PenguinNav.SnapPositionToApproximateGround(request.End);

            NavPath outputPath;
            state.PoolLock.AcquireWrite();
            outputPath = state.PathPool.Alloc();
            state.PoolLock.ReleaseWrite();

            outputPath.Positions.Clear();

            bool validPath;
            bool initialRaycast;
            using(Profiling.Time("Initial Walk Raycast", ProfileTimeUnits.Microseconds)) {
                initialRaycast = PenguinNav.IsWalkableRaycast(start, end);
            }

            if (initialRaycast) {
                outputPath.Positions.PushBack(end);
                validPath = true;
            } else {
                NodePath p = state.WorkPath;
                p.Reset();

                ushort closestStart, closestEnd;
                bool foundNodes;

                using (Profiling.Time("Locating Start/End Nodes", ProfileTimeUnits.Microseconds)) {
                    foundNodes = PenguinNav.TryFindClosestWalkableNodeToPoint(start, out closestStart);
                    foundNodes &= PenguinNav.TryFindClosestWalkableNodeToPoint(end, out closestEnd);
                }

                if (!foundNodes) {
                    validPath = false;
                } else {
                    bool found;
                    using (Profiling.Time("Pathing A*", ProfileTimeUnits.Microseconds)) {
                        found = Pathfinder.AStar(PenguinNav.Mesh.Graph, ref p, closestStart, closestEnd);
                    }

                    if (found) {
                        validPath = true;

                        int traversalCount = p.Length();
                        Vector3 simplifyRoot = start;

                        using (Profiling.Time("Path Simplification", ProfileTimeUnits.Microseconds)) {
                            for (int i = 0; i < traversalCount; i++) {
                                var traversal = p.Traversal(i);
                                Vector3 nextPos = PenguinNav.Mesh.Graph.Node(traversal.NodeId).Position;
                                if (i > 0) {
                                    if (PenguinNav.IsWalkableRaycast(simplifyRoot, nextPos)) {
                                        outputPath.Positions.PopBack();
                                    } else {
                                        simplifyRoot = outputPath.Positions.PeekBack();
                                    }
                                }
                                outputPath.Positions.PushBack(nextPos);
                            }

                            if (traversalCount > 1 && PenguinNav.IsWalkableRaycast(simplifyRoot, end)) {
                                outputPath.Positions.PopBack();
                            }

                            outputPath.Positions.PushBack(end);
                        }
                    } else {
                        validPath = false;
                    }
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
                PenguinPathQueuedResponse response;
                response.Handler = request.OnPathResolved;
                response.Context = request.Context;
                response.Path = outputPath;
                state.ResponseLock.AcquireWrite();
                state.ResponseBuffer.PushBack(response);
                state.ResponseLock.ReleaseWrite();
            }
        }
    }
}