using FieldDay.SharedState;

namespace Pennycook {
    public sealed class PlayerProgressState : ISharedState
    {
        public int DayIndex;
        public PlayerProgressMode Mode;
        public int CulturalVersion = -1;
        public int ScriptVersion = -1;
    }

    public enum PlayerProgressMode {
        Title,
        Gameplay,
        Sandbox
    }
}