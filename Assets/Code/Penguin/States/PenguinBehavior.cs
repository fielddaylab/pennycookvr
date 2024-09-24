using System.Runtime.CompilerServices;
using FieldDay.Processes;

namespace Pennycook {
    public abstract class PenguinProcessState : IProcessStateEnterExit {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static protected PenguinBrain Brain(Process p) {
            return p.Context<PenguinBrain>();
        }

        public virtual void OnEnter(Process p) { }
        public virtual void OnExit(Process p) { }
    }

    public abstract class ParameterizedPenguinState<TParam> : PenguinProcessState where TParam : unmanaged {
        public virtual void OnEnter(Process p, ref TParam param) { }

        public override void OnEnter(Process p) {
            OnEnter(p, ref p.Data<TParam>());
        }
    }
}