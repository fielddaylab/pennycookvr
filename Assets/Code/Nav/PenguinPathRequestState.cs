using System;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Graph;
using FieldDay;
using FieldDay.SharedState;
using FieldDay.Threading;
using UnityEngine;

namespace Pennycook {
    /// <summary>
    /// Path request status.
    /// </summary>
    [SharedStateInitOrder(10)]
    public sealed class PenguinPathRequestState : SharedStateComponent, IRegistrationCallbacks {
        [NonSerialized] public RingBuffer<PenguinPathRequest> RequestBuffer;
        [NonSerialized] public AtomicRWLock RequestLock;

        [NonSerialized] public RingBuffer<PenguinPathQueuedResponse> ResponseBuffer;
        [NonSerialized] public AtomicRWLock ResponseLock;

        [NonSerialized] public UniqueIdAllocator16 RequestIdGenerator;
        [NonSerialized] public AtomicRWLock RequestIdLock;

        [NonSerialized] public IPool<NavPath> PathPool;
        [NonSerialized] public AtomicRWLock PoolLock;

        [NonSerialized] public NodePath WorkPath;

        [NonSerialized] public AsyncHandle RequestHandler;

        void IRegistrationCallbacks.OnRegister() {
            RequestBuffer = new RingBuffer<PenguinPathRequest>(64, RingBufferMode.Fixed);
            ResponseBuffer = new RingBuffer<PenguinPathQueuedResponse>(64, RingBufferMode.Fixed);
            RequestIdGenerator = new UniqueIdAllocator16(64, false);
            PathPool = new FixedPool<NavPath>(64, Pool.DefaultConstructor<NavPath>());
            PathPool.Prewarm();

            WorkPath = new NodePath(32);
        }

        void IRegistrationCallbacks.OnDeregister() {
        }
    }

    /// <summary>
    /// Pathfinding request.
    /// </summary>
    public struct PenguinPathRequest {
        public Vector3 Start;
        public Vector3 End;
        public UniqueId16 Id;
        public PenguinPathResponseHandler OnPathResolved;
        public object Context;
    }

    /// <summary>
    /// Pathfinding response.
    /// </summary>
    public struct PenguinPathQueuedResponse {
        public NavPath Path;
        public PenguinPathResponseHandler Handler;
        public object Context;
    }
    
    /// <summary>
    /// Handler for the completion of a pathfinding request.
    /// </summary>
    public delegate void PenguinPathResponseHandler(NavPath path, object context);

    static public partial class PenguinNav {
        [SharedStateReference]
        static public PenguinPathRequestState Requests { get; private set; }

        /// <summary>
        /// Returns if the given path request is running.
        /// </summary>
        static public bool IsPathRequestRunning(UniqueId16 requestId) {
            Atomics.AcquireRead(ref Requests.RequestIdLock);
            bool isAlive = Requests.RequestIdGenerator.IsValid(requestId);
            Atomics.ReleaseRead(ref Requests.RequestIdLock);
            return isAlive;
        }

        /// <summary>
        /// Requests a path from one location to another.
        /// </summary>
        static public UniqueId16 RequestPath(Vector3 start, Vector3 end, PenguinPathResponseHandler onResolve, object context) {
            Assert.True(onResolve != null, "Path request must have a response handler");

            PenguinPathRequest request;
            request.Start = start;
            request.End = end;
            request.OnPathResolved = onResolve;
            request.Context = context;

            Requests.RequestIdLock.AcquireWrite();
            request.Id = Requests.RequestIdGenerator.Alloc();
            Requests.RequestIdLock.ReleaseWrite();

            Requests.RequestLock.AcquireWrite();
            Requests.RequestBuffer.PushBack(request);
            Requests.RequestLock.ReleaseWrite();

            return request.Id;
        }

        /// <summary>
        /// Cancels the pathfinding request with the given handle.
        /// </summary>
        static public void CancelPath(ref UniqueId16 requestHandle) {
            if (requestHandle.Id == 0) {
                return;
            }

            Requests.RequestIdLock.AcquireWrite();
            Requests.RequestIdGenerator.Free(requestHandle);
            Requests.RequestIdLock.ReleaseWrite();
            requestHandle = default;
        }

        /// <summary>
        /// Frees a nav path.
        /// </summary>
        static public void FreeNavPath(ref NavPath path) {
            if (path == null) {
                return;
            }

            Requests.PoolLock.AcquireWrite();
            Requests.PathPool.Free(path);
            Requests.PoolLock.ReleaseWrite();
            path = null;
        }
    }
}