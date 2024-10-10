using FieldDay;
using FieldDay.Pipes;
using FieldDay.SharedState;
using FieldDay.Systems;
using UnityEngine;

namespace Pennycook.Environment {
    [SysUpdate(GameLoopPhase.UnscaledLateUpdate, 1000)]
    public class FootstepDecalSpawnSystem : SharedStateSystemBehaviour<FootstepDecalSpawner> {
        public override void ProcessWork(float deltaTime) {
            while(m_State.DecalRequests.TryRead(out FootstepDecalRequest decalRequest)) {
                Vector3 pos = decalRequest.Location.position;

                if (!PenguinNav.GetAccurateTerrainAt(pos, out float posY, out Vector3 normal)) {
                    continue;
                }

                pos.y = posY;
            }
        }
    }
}