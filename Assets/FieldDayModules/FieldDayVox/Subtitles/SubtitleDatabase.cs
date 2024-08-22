using System.Collections;
using System.Collections.Generic;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Streaming;
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

    static public class SubtitleUtility {
        [SharedStateReference] static public SubtitleDatabase DB { get; private set; }
    }
}