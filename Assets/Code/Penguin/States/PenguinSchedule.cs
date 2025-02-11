using System.Collections;
using BeauUtil;
using FieldDay.Processes;

namespace Pennycook {
    public abstract class PenguinSchedule : PenguinProcessState, IProcessStateSequence, IProcessStateSignal {
        public virtual void OnSignal(Process process, StringHash32 signalId, object signalArgs) {
        }

        public virtual IEnumerator Sequence(Process process) {
            return null;
        }
    }

    static public class PenguinSchedules {
        static public readonly ProcessStateDefinition Wander = ProcessStateDefinition.FromCallbacks("Wander", new PenguinWanderSchedule());
        static public readonly ProcessStateDefinition MatingDance = ProcessStateDefinition.FromCallbacks("MatingDance", new PenguinMatingSchedule());
        static public readonly ProcessStateDefinition Regurgitation = ProcessStateDefinition.FromCallbacks("Regurgitation", new PenguinRegurgitationSchedule());
        static public readonly ProcessStateDefinition WeighGate = ProcessStateDefinition.FromCallbacks("WeighGate", new PenguinWeighGateSchedule());
    }
}