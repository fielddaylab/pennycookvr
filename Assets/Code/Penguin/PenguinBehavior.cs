using System.Runtime.CompilerServices;
using FieldDay.Processes;

namespace Pennycook {
    public abstract class PenguinProcessState : IProcessStateCallbacks {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static protected PenguinBrain Brain(Process p) {
            return p.Context<PenguinBrain>();
        }
    }
}