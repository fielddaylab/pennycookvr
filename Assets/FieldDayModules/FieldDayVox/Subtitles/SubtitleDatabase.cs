using System.Collections;
using System.Collections.Generic;
using BeauRoutine;
using BeauUtil;
using FieldDay.Scenes;
using FieldDay.SharedState;
using UnityEngine;

namespace FieldDay.Vox {
    public class SubtitleDatabase : ISharedState, IRegistrationCallbacks, ISceneLoadDependency {
        internal Dictionary<StringHash32, string> TextMap = new Dictionary<StringHash32, string>();
        internal AsyncHandle FileLoader;

        #region IRegistrationCallbacks

        void IRegistrationCallbacks.OnDeregister() {
            Game.Scenes?.DeregisterLoadDependency(this);
        }

        void IRegistrationCallbacks.OnRegister() {
            Game.Scenes.RegisterLoadDependency(this);
        }

        #endregion // IRegistrationCallbacks

        #region ISceneLoadDependency

        bool ISceneLoadDependency.IsLoaded(SceneLoadPhase loadPhase) {
            if (loadPhase == SceneLoadPhase.BeforeReady) {
                return !FileLoader.IsRunning();
            } else {
                return true;
            }
        }

        #endregion // ISceneLoadDependency
    }

    static public partial class SubtitleUtility {
        [SharedStateReference] static public SubtitleDatabase DB { get; private set; }

        [InvokePreBoot]
        static private void Initialize() {
            Game.SharedState.Register(new SubtitleDatabase());
        }

        /// <summary>
        /// Finds the subtitle for the given line.
        /// </summary>
        static public string FindSubtitle(StringHash32 lineCode, string fallback) {
            string text;
            if (!DB.TextMap.TryGetValue(lineCode, out text)) {
                text = fallback;
            }
            return text;
        }

        #region Parser

        // TODO: Implement subtitle file parsing
        // probably CSV file
        // possibly split it into multiple pages/chunks
        // so we don't have to have all subtitles loaded all the time

        #endregion // Parser
    }
}