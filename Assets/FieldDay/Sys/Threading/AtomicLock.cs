#if !UNITY_WEBGL
#define SUPPORTS_THREADING
#endif // !UNITY_WEBGL

using BeauUtil;
using BeauUtil.Debugger;
using System.Threading;

namespace FieldDay.Threading {
    public struct AtomicLock {
        internal long m_Value;
        
        internal const long WriteValue = 0x7FFFFFFF;
    }

    static public class Atomics {
        static public void AcquireRead(ref AtomicLock atomic) {
#if SUPPORTS_THREADING
            while (Interlocked.Read(ref atomic.m_Value) == AtomicLock.WriteValue)
                continue;

            Interlocked.Increment(ref atomic.m_Value);
#else
            Assert.True(atomic.m_Value != AtomicLock.WriteValue);
            atomic.m_Value = AtomicLock.WriteValue;
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

        static public void AcquireWrite(ref AtomicLock atomic) {
#if SUPPORTS_THREADING
            while (Interlocked.CompareExchange(ref atomic.m_Value, AtomicLock.WriteValue, 0) != 0)
                continue;
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

        static public bool CanAcquireRead(ref AtomicLock atomic) {
#if SUPPORTS_THREADING
            return Interlocked.Read(ref atomic.m_Value) != AtomicLock.WriteValue;
#else
            return atomic.m_Value != AtomicLock.WriteValue;
#endif // SUPPORTS_THREADING
        }
    }
}