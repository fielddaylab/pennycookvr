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
            while (Interlocked.Read(ref atomic.m_Value) == AtomicLock.WriteValue)
                continue;

            Interlocked.Increment(ref atomic.m_Value);
        }

        static public void ReleaseRead(ref AtomicLock atomic) {
            Assert.True(Interlocked.Read(ref atomic.m_Value) > 0, "Atomics.ReleaseRead unbalanced");
            Interlocked.Decrement(ref atomic.m_Value);
        }

        static public void AcquireWrite(ref AtomicLock atomic) {
            while (Interlocked.CompareExchange(ref atomic.m_Value, AtomicLock.WriteValue, 0) != 0)
                continue;
        }

        static public void ReleaseWrite(ref AtomicLock atomic) {
            Assert.True(Interlocked.Read(ref atomic.m_Value) == AtomicLock.WriteValue, "Atomics.ReleaseWrite unbalanced");
            Interlocked.Exchange(ref atomic.m_Value, 0);
        }
    }
}