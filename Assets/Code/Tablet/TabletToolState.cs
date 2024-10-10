using System;
using BeauUtil;
using BeauUtil.UI;
using BeauUtil.Variants;
using FieldDay;
using FieldDay.Audio;
using FieldDay.Scripting;
using FieldDay.SharedState;
using UnityEngine;
using UnityEngine.UI;

namespace Pennycook.Tablet {
    public class TabletToolState : SharedStateComponent, IRegistrationCallbacks {
        static public readonly TableKeyPair Var_CurrentTool = TableKeyPair.Parse("tablet:tool");

        [Serializable]
        public struct ToolConfig {
            public TabletTool Tool;
            public ModeLabelDisplay Label;
            public Color ThemeColor;
        }

        [Header("Config")]
        public ToolConfig[] Configs;
        public ShapeGraphic Outline;
        public Graphic[] ToolColorTinted;

        [Header("State")]
        public TabletTool CurrentTool;
        public bool AllowToolSwitch = true;

        [NonSerialized] public int CurrentToolIndex = -1;
        [NonSerialized] public TabletToolDefinition CurrentToolDef = TabletToolDefinitions.None;

        void IRegistrationCallbacks.OnDeregister() {
            ScriptUtility.UnbindVariable(Var_CurrentTool);
        }

        void IRegistrationCallbacks.OnRegister() {
            TabletUtility.SetTool(this, TabletUtility.IndexOfTool(this, CurrentTool), false);

            ScriptUtility.BindVariable(Var_CurrentTool, () => TabletUtility.TabletToolToStringHash[(int) CurrentTool]);
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
        static public readonly StringHash32[] TabletToolToStringHash = { "None", "Scan", "Capture", "Count", "Move" };
        
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

                foreach(var graphic in toolState.ToolColorTinted) {
                    graphic.color = config.ThemeColor;
                }

            } else {
                toolState.Outline.enabled = false;
            }

            toolState.CurrentTool = tool;
            toolState.CurrentToolDef = TabletToolDefinitions.Get(tool);

            if (playFeedback) {
                TabletUtility.PlaySfx("Tablet.ModeChanged");
                TabletUtility.PlayHaptics(0.1f, 0.01f);
                using (var t = TempVarTable.Alloc()) {
                    t.Set("toolId", TabletToolToStringHash[(int) tool]);
                    ScriptUtility.Trigger(TabletTriggers.ChangedTabletTool, t);
                }
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