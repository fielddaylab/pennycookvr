using System;
using FieldDay;
using FieldDay.Components;
using FieldDay.Systems;
using UnityEngine;

namespace Pennycook.Tablet {
    [SysUpdate(GameLoopPhase.UnscaledLateUpdate, 10000)]
    public class FixedCameraRefreshSystem : ComponentSystemBehaviour<FixedCameraRefreshRate> {
        public override void ProcessWork(float deltaTime) {
            if (Game.Scenes.IsMainLoading()) {
                foreach (var c in m_Components) {
                    foreach(var camera in c.Cameras) {
                        camera.enabled = false;
                    }
                }
                return;
            }

            foreach (var c in m_Components) {
                if (c.Paused) {
                    continue;
                }

                if ((c.TimeBeforeNextRefresh -= deltaTime) <= 0) {
                    foreach (var camera in c.Cameras) {
                        camera.enabled = true;
                    }
                }
            }
        }

        private void OnFrameAdvance() {
            if (Game.Scenes.IsMainLoading()) {
                return;
            }

            foreach (var c in m_Components) {
                if (c.TimeBeforeNextRefresh <= 0) {
                    c.TimeBeforeNextRefresh += 1f / c.RefreshRate;
                    foreach (var camera in c.Cameras) {
                        camera.enabled = false;
                    }
                }
            }
        }

        public override void Initialize() {
            base.Initialize();

            GameLoop.OnFrameAdvance.Register(OnFrameAdvance);
        }

        public override void Shutdown() {
            GameLoop.OnFrameAdvance.Deregister(OnFrameAdvance);

            base.Shutdown();
        }
    }
}