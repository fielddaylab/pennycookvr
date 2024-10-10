using System;
using BeauUtil;
using FieldDay;
using FieldDay.Pipes;
using FieldDay.SharedState;
using UnityEngine;

namespace Pennycook.Environment {
    public class FootstepDecalSpawner : SharedStateComponent {
        public Pipe<FootstepDecalRequest> DecalRequests = new Pipe<FootstepDecalRequest>(32, false);
    }

    public struct FootstepDecalRequest {
        public Pose Location;
        public FootstepDecalType Type;
    }

    [Flags]
    public enum FootstepDecalType {
        Penguin = 0x01,
        PenguinChick = 0x02,

        [Hidden] LeftFoot = 0,
        [Hidden] RightFoot = 0x10
    }

    static public partial class VFXUtility {
        static public void QueueFootstepDecal(Transform position, FootstepDecalType type) {
            FootstepDecalRequest request;
            position.GetPositionAndRotation(out request.Location.position, out request.Location.rotation);
            request.Type = type;

            Find.State<FootstepDecalSpawner>().DecalRequests.Write(request);
        }
    }
}