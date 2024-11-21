using FieldDay.SharedState;

namespace Pennycook {
    public sealed class PlayerProgressState : ISharedState {
        public int DayIndex;
        public PlayerProgressMode Mode;
    }

    public enum PlayerProgressMode {
        Title,
        Gameplay,
        Sandbox
    }
}