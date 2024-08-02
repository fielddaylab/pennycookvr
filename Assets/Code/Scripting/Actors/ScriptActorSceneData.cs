using System;
using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using FieldDay.Scenes;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FieldDay.Scripting {
    [AddComponentMenu("")]
    public sealed class ScriptActorSceneData : SceneCustomData {
        [SerializeField] private ScriptActor[] m_InitiallyDeactivatedObjects;

        [NonSerialized] private int[] m_CachedObjectsIds;

#if UNITY_EDITOR
        public override bool Build(Scene scene) {
            List<ScriptActor> actors = new List<ScriptActor>(32);
            SceneHelper.GetAllComponents(scene, true, actors);
            actors.RemoveAll((a) => a.isActiveAndEnabled);

            if (actors.Count > 0) {
                m_InitiallyDeactivatedObjects = actors.ToArray();
                return true;
            } else {
                m_InitiallyDeactivatedObjects = Array.Empty<ScriptActor>();
                return true;
            }
        }
#endif // UNITY_EDITOR

        public override void OnLateEnable() {
            using (PooledList<int> cachedObjectIds = PooledList<int>.Create()) {
                foreach (var actor in m_InitiallyDeactivatedObjects) {
                    if (actor) {
                        cachedObjectIds.Add(UnityHelper.Id(actor));
                        // TODO: Register
                    }
                }
                m_CachedObjectsIds = cachedObjectIds.ToArray();
            }
        }

        public override void OnUnload() {
            foreach (var id in m_CachedObjectsIds) {
                if (UnityHelper.IsAlive(id)) {
                    var actor = UnityHelper.Find<ScriptActor>(id);
                    // TODO: Deregister
                }
            }
        }
    }
}