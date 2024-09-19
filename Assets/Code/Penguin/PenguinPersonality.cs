using FieldDay.Assets;
using FieldDay.Filters;
using UnityEngine;

namespace Pennycook {
    [CreateAssetMenu(menuName = "Pennycook/Penguin/Personality")]
    public sealed class PenguinPersonality : NamedAsset {
        [Header("Player Tolerance")]
        public float PlayerDistanceBubble = 8;
        public float PlayerCloseDistanceBubble = 0.5f;
        public SignalLatchWindow PlayerAnnoyanceLatching = SignalLatchWindow.Full;
        public SignalEnvelope PlayerAnnoyanceEnvelope = new SignalEnvelope(10, 8);
        public SignalEnvelope PlayerCloseAnnoyanceEnvelope = new SignalEnvelope(6, 16);

        // TODO: Margo sound curiosity

        // TODO: Mate defensiveness?
        // TODO: Preferred distance from other penguins
        // TODO: Wander parameters/restlessness

        //[Header("Sound Curiosity")]
        //public float SoundDistanceBubble = 
    }
}