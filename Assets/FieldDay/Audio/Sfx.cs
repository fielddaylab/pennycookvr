using BeauUtil;
using UnityEngine;

namespace FieldDay.Audio {
    static public class Sfx {
        static public void OneShot(StringHash32 eventId, Transform position) {
            Game.Audio.QueuePlayAudioCommand(new AudioCommand() {
                Type = AudioCommandType.PlayClipFromName,
                Play = new PlayCommandData() {
                    Asset = eventId,
                    TransformOrAudioSourceId = UnityHelper.Id(position),
                    Volume = 1,
                    Pitch = 1,
                    RotationOffset = Quaternion.identity,
                }
            });
        }
    }
}