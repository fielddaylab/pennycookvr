using BeauUtil;
using FieldDay.Vox;
using FieldDay.VRHands;
using UnityEngine;

public class VoxTest : MonoBehaviour {
    public VoxEmitter Vox;
    public SerializedHash32 Line;
    public Grabbable Grabbable;

    public void Awake() {
        Grabbable.OnGrabbed.Register((a) => {
            VoxUtility.QueueLoad(Line);
        });

        VoxUtility.QueueLoad(Line);
    }
}