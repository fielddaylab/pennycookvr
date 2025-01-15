using System;
using System.Collections.Generic;
using BeauUtil;
using FieldDay;
using FieldDay.Scenes;
using FieldDay.Scripting;
using UnityEngine;

namespace Pennycook {
    public sealed class DayScriptLoader : MonoBehaviour, IScenePreload, ISceneUnloadHandler, IDynamicSceneImport {
        [NonSerialized] private UniqueId16[] m_LoadHandles;

        void ISceneUnloadHandler.OnSceneUnload(SceneBinding inScene, object inContext) {
            if (m_LoadHandles != null) {
                for (int i = 0; i < m_LoadHandles.Length; i++) {
                    ScriptDBUtility.Unload(m_LoadHandles[i]);
                }
            }
        }

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            DayConfigAsset config = UniverseUtility.GetConfigForCurrentState();

            m_LoadHandles = new UniqueId16[config.Scripts.Length];
            for (int i = 0; i < config.Scripts.Length; i++) {
                m_LoadHandles[i] = ScriptDBUtility.Load(config.Scripts[i]);
            }

            UniverseUtility.LoadGoals(config);

            return null;
        }

        IEnumerable<SceneImportSettings> IDynamicSceneImport.GetSubscenes()
        {
            DayConfigAsset config = UniverseUtility.GetConfigForCurrentState();

            for(int i = 0; i < config.AuxScenes.Length; i++) {
                 yield return new SceneImportSettings(config.AuxScenes[i], SceneImportFlags.Auxillary);
            }
        }

    }
}