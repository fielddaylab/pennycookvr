#if !UNITY_WEBGL
#define SUPPORTS_THREADING
#endif // !UNITY_WEBGL

using BeauUtil;
using BeauUtil.Debugger;
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace FieldDay.Threading {
    public struct AtomicLock {
        [NonSerialized] internal long m_Value;
        
        internal const long WriteValue = 0x7FFFFFFF;

        #region Read

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AcquireRead() {
            Atomics.AcquireRead(ref this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAcquireRead() {
            return Atomics.TryAcquireRead(ref this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReleaseRead() {
            Atomics.ReleaseRead(ref this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanAcquireRead() {
            return Atomics.CanAcquireRead(ref this);
        }

        #endregion // Read

        #region Write

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AcquireWrite() {
            Atomics.AcquireWrite(ref this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReleaseWrite() {
            Atomics.ReleaseWrite(ref this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanAcquireWrite() {
            return Atomics.CanAcquireWrite(ref this);
        }

        #endregion // Write
    }

    static public class Atomics {
        #region Read

        static public void AcquireRead(ref AtomicLock atomic) {
#if SUPPORTS_THREADING
            while (Interlocked.Read(ref atomic.m_Value) == AtomicLock.WriteValue) {
                Console.WriteLine("read spin");
            }

            Interlocked.Increment(ref atomic.m_Value);
#else
            Assert.True(atomic.m_Value != AtomicLock.WriteValue);
            atomic.m_Value = AtomicLock.WriteValue;
#endif // SUPPORTS_THREADING
        }

        static public bool TryAcquireRead(ref AtomicLock atomic) {
#if SUPPORTS_THREADING
            if (Interlocked.Read(ref atomic.m_Value) == AtomicLock.WriteValue)
                return false;

            Interlocked.Increment(ref atomic.m_Value);
            return true;
#else
            if (atomic.m_Value == AtomicLock.WriteValue) {
                return false;
            }

            atomic.m_Value = AtomicLock.WriteValue;
            return true;
#endif // SUPPORTS_THREADING
        }

        static public void ReleaseRead(ref AtomicLock atomic) {
#if SUPPORTS_THREADING
            Assert.True(Interlocked.Read(ref atomic.m_Value) > 0, "Atomics.ReleaseRead unbalanced");
            Interlocked.Decrement(ref atomic.m_Value);
#else
            Assert.True(atomic.m_Value > 0, "Atomics.ReleaseRead unbalanced");
            atomic.m_Value--;
#endif // SUPPORTS_THREADING
        }

        static public bool CanAcquireRead(ref AtomicLock atomic) {
#if SUPPORTS_THREADING
            return Interlocked.Read(ref atomic.m_Value) != AtomicLock.WriteValue;
#else
            return atomic.m_Value != AtomicLock.WriteValue;
#endif // SUPPORTS_THREADING
        }

        #endregion // Read

        #region Write

        static public void AcquireWrite(ref AtomicLock atomic) {
#if SUPPORTS_THREADING
            while (Interlocked.CompareExchange(ref atomic.m_Value, AtomicLock.WriteValue, 0) != 0) {
                Console.WriteLine("write spin");
            }
#else
            Assert.True(atomic.m_Value == 0);
            atomic.m_Value = AtomicLock.WriteValue;
#endif // SUPPORTS_THREADING
        }

        static public void ReleaseWrite(ref AtomicLock atomic) {
#if SUPPORTS_THREADING
            Assert.True(Interlocked.Read(ref atomic.m_Value) == AtomicLock.WriteValue, "Atomics.ReleaseWrite unbalanced");
            Interlocked.Exchange(ref atomic.m_Value, 0);
#else
            Assert.True(atomic.m_Value == AtomicLock.WriteValue, "Atomics.ReleaseWrite unbalanced");
            atomic.m_Value = 0;
#endif // SUPPORTS_THREADING
        }



        static public bool CanAcquireWrite(ref AtomicLock atomic) {
#if SUPPORTS_THREADING
            return Interlocked.Read(ref atomic.m_Value) == 0;
#else
            return atomic.m_Value == 0;
#endif // SUPPORTS_THREADING
        }

        #endregion // Write
    }
}