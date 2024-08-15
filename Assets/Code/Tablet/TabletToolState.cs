using System;
using BeauUtil.UI;
using FieldDay;
using FieldDay.SharedState;
using UnityEngine;

namespace Pennycook.Tablet {
    public class TabletToolState : SharedStateComponent, IRegistrationCallbacks {
        [Serializable]
        public struct ToolConfig {
            public TabletTool Tool;
            public ModeLabelDisplay Label;
            public Color ThemeColor;
        }

        [Header("Config")]
        public ToolConfig[] Configs;
        public RectGraphic Outline;

        [Header("State")]
        public TabletTool CurrentTool;
        public bool AllowToolSwitch = true;

        [NonSerialized] public int CurrentToolIndex = -1;

        void IRegistrationCallbacks.OnDeregister() {
        }

        void IRegistrationCallbacks.OnRegister() {
            TabletUtility.SetTool(this, TabletUtility.IndexOfTool(this, CurrentTool), false);
        }
    }

    public enum TabletTool {
        None,
        Scan,
        Capture,
        Count,
        Move,
    }

    static public partial class TabletUtility {
        static public void SetTool(TabletToolState toolState, int index, bool playFeedback) {
            int prevIdx = toolState.CurrentToolIndex;
            if (prevIdx == index) {
                return;
            }

            if (prevIdx >= 0) {
                toolState.Configs[prevIdx].Label.SetState(false);
            }

            toolState.CurrentToolIndex = index;

            TabletTool tool = TabletTool.None;

            if (index >= 0) {
                var config = toolState.Configs[index];
                config.Label.SetState(true);
                tool = config.Tool;
                toolState.Outline.color = config.ThemeColor;
                toolState.Outline.enabled = true;
            } else {
                toolState.Outline.enabled = false;
            }

            toolState.CurrentTool = tool;

            if (playFeedback) {
                // TODO: play sound
            }
        }
    
        static public int IndexOfTool(TabletToolState toolState, TabletTool tool) {
            var configs = toolState.Configs;
            for(int i = 0; i < configs.Length; i++) {
                if (configs[i].Tool == tool) {
                    return i;
                }
            }

            return -1;
        }
    }
}