using BeauUtil;
using FieldDay.Vox;
using FieldDay.VRHands;
using UnityEngine;

public class VoxTest : MonoBehaviour {
    public VoxEmitter Vox;
    public SerializedHash32 Line;
    public Grabbable Grabbable;
    public string Subtitle;

    public void Start() {
        Grabbable.OnGrabbed.Register((a) => {
            VoxUtility.Speak(Vox, Line, Subtitle);
        });

        VoxUtility.AddHumanReadableMapping(Line, Line.Source());
    }
}