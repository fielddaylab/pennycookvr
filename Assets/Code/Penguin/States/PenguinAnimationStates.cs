using FieldDay.Processes;

namespace Pennycook {
    public sealed class PenguinIdleState : PenguinProcessState { }

    static public class PenguinStates {
        static public readonly ProcessStateDefinition Idle = ProcessStateDefinition.FromCallbacks("Idle", new PenguinIdleState());
        static public readonly ProcessStateDefinition Walking = ProcessStateDefinition.FromCallbacks("Walk", new PenguinWalkState());

        static public readonly ProcessStateDefinition Dancing = ProcessStateDefinition.FromCallbacks("Dance", new PenguinDanceState());

        static public readonly ProcessStateDefinition Regurgitating = ProcessStateDefinition.FromCallbacks("Regurgitate", new PenguinRegurgitationState());

        static public readonly ProcessStateDefinition WeighGate = ProcessStateDefinition.FromCallbacks("WeighGate", new PenguinWeighGateState());
    }
}