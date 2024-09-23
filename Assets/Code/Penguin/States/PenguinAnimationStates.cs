using FieldDay.Processes;

namespace Pennycook {
    public sealed class PenguinIdleState : PenguinProcessState { }

    static public class PenguinStates {
        static public readonly ProcessStateDefinition Idle = ProcessStateDefinition.FromCallbacks("Idle", new PenguinIdleState());
        static public readonly ProcessStateDefinition Walking = ProcessStateDefinition.FromCallbacks("Walk", new PenguinWalkState());
    }
}