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
    [SharedStateInitOrder(10)]
    public sealed class PenguinPathfinderState : SharedStateComponent, IRegistrationCallbacks {
        [NonSerialized] public RingBuffer<PenguinPathRequest> RequestBuffer;
        [NonSerialized] public AtomicLock BufferLock;

        [NonSerialized] public UniqueIdAllocator16 RequestIdGenerator;
        [NonSerialized] public AtomicLock RequestIdLock;

        [NonSerialized] public IPool<NavPath> PathPool;
        [NonSerialized] public AtomicLock PoolLock;

        [NonSerialized] public NodePath WorkPath;

        [NonSerialized] public AsyncHandle RequestHandler;

        void IRegistrationCallbacks.OnRegister() {
            RequestBuffer = new RingBuffer<PenguinPathRequest>(64, RingBufferMode.Fixed);
            RequestIdGenerator = new UniqueIdAllocator16(64, false);
            PathPool = new FixedPool<NavPath>(64, Pool.DefaultConstructor<NavPath>());
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
        public Action<NavPath> OnPathResolved;
    }

    static public partial class PenguinUtility {
        [SharedStateReference]
        static public PenguinPathfinderState Pathfinder { get; private set; }

        /// <summary>
        /// Returns if the given path request is alive.
        /// </summary>
        static public bool IsPathRequestAlive(UniqueId16 requestId) {
            Atomics.AcquireRead(ref Pathfinder.RequestIdLock);
            bool isAlive = Pathfinder.RequestIdGenerator.IsValid(requestId);
            Atomics.ReleaseRead(ref Pathfinder.RequestIdLock);
            return isAlive;
        }

        /// <summary>
        /// Requests a path from one location to another.
        /// </summary>
        static public UniqueId16 RequestPath(Vector3 start, Vector3 end, Action<NavPath> onResolve) {
            Assert.True(onResolve != null, "Path request must have a response handler");

            PenguinPathRequest request;
            request.Start = start;
            request.End = end;
            request.OnPathResolved = onResolve;

            Pathfinder.RequestIdLock.AcquireWrite();
            request.Id = Pathfinder.RequestIdGenerator.Alloc();
            Pathfinder.RequestIdLock.ReleaseWrite();

            Pathfinder.BufferLock.AcquireWrite();
            Pathfinder.RequestBuffer.PushBack(request);
            Pathfinder.BufferLock.ReleaseWrite();

            return request.Id;
        }

        /// <summary>
        /// Cancels the pathfinding request with the given handle.
        /// </summary>
        static public void CancelPath(UniqueId16 requestHandle) {
            if (requestHandle.Id == 0) {
                return;
            }

            Pathfinder.RequestIdLock.AcquireWrite();
            Pathfinder.RequestIdGenerator.Free(requestHandle);
            Pathfinder.RequestIdLock.ReleaseWrite();
        }
    }
}