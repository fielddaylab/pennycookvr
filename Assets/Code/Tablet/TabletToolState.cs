using System;
using BeauUtil;
using BeauUtil.UI;
using BeauUtil.Variants;
using FieldDay;
using FieldDay.Animation;
using FieldDay.Audio;
using FieldDay.Scripting;
using FieldDay.SharedState;
using FieldDay.UI;
using FieldDay.UI.Animation;
using UnityEngine;
using UnityEngine.UI;

namespace Pennycook.Tablet {
    public class TabletToolState : SharedStateComponent, IRegistrationCallbacks {
        static public readonly TableKeyPair Var_CurrentTool = TableKeyPair.Parse("tablet:tool");

        [Serializable]
        public struct ToolConfig {
            public TabletTool Tool;
            public ModeLabelDisplay Label;
            public SidePanelDisplay SidePanel;
            public Color ThemeColor;
            public Color SecondThemeColor;
        }

        [Header("Config")]
        public ToolConfig[] Configs;
        public ShapeGraphic Outline;
        public Graphic[] ToolColorTinted;

        [Header("Interface")]
        public FadeGroup Reticle;
        public RectTransform TabLayout;
        public LayoutListener TabLayoutListener;
        public FadeGroup CountGroup;
        public FadeGroup CaptureGroup;
        public Camera DetailCamera;

        [Header("State")]
        public TabletTool CurrentTool;
        public bool AllowToolSwitch = true;

        public bool NoCount = false;

        [NonSerialized] public int CurrentToolIndex = -1;
        [NonSerialized] public TabletToolDefinition CurrentToolDef = TabletToolDefinitions.None;

        void IRegistrationCallbacks.OnDeregister() {
            ScriptUtility.UnbindVariable(Var_CurrentTool);
        }

        void IRegistrationCallbacks.OnRegister() {
            TabletUtility.SetTool(this, TabletUtility.IndexOfTool(this, CurrentTool), false);

            ScriptUtility.BindVariable(Var_CurrentTool, () => TabletUtility.TabletToolToStringHash[(int) CurrentTool]);

            TabLayoutListener.OnPostLayout.Register(OnTabLayoutAdjusted);
        }

        private void OnTabLayoutAdjusted() {
            if (CurrentToolIndex < 0) {
                TabLayout.anchoredPosition = default;
            } else {
                float adjust = TabLayout.sizeDelta.x / 2;
                RectTransform buttonTransform = (RectTransform) Configs[CurrentToolIndex].Label.transform;
                adjust -= buttonTransform.anchoredPosition.x;
                TabLayout.anchoredPosition = new Vector2(adjust, 0);
            }
        }

        public void SetNoCount(bool NC)
        {
            NoCount = NC;
            Configs[Configs.Length-1].Label.gameObject.SetActive(!NC);
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

            TabletHighlightState highlights = Find.State<TabletHighlightState>();
            TabletControlState ctrl = Find.State<TabletControlState>();

            TabletUtility.LoadGoals(index);

            if (prevIdx >= 0) {
                toolState.Configs[prevIdx].Label.SetState(false);
                toolState.Configs[prevIdx].SidePanel.SetState(false);
                if(toolState.Configs[prevIdx].SidePanel.PairedPanel != null) {
                    toolState.Configs[prevIdx].SidePanel.PairedPanel.SetState(false);
					toolState.Configs[prevIdx].SidePanel.PairedPanel.SetSummaryGoals(false);
                }

                var oldTool = TabletToolDefinitions.Get(toolState.CurrentTool);
                oldTool.OnUnhighlighted?.Invoke(highlights.HighlightedObject, ctrl);

                oldTool.OnClose?.Invoke(ctrl);
            }

            toolState.CurrentToolIndex = index;

            TabletTool tool = TabletTool.None;

            if (index >= 0) {
                var config = toolState.Configs[index];
                config.Label.SetState(true);
                config.SidePanel.SetState(true);
                if(config.SidePanel.PairedPanel != null) {
                    if(TabletUtility.GoalOfTypeExists(config.SidePanel.PairedPanel.Type)) {
                        config.SidePanel.PairedPanel.SetState(true);
						config.SidePanel.PairedPanel.SetSummaryGoals(true);
                        config.SidePanel.gameObject.transform.SetAsFirstSibling();
                    }
                }
                
                tool = config.Tool;
                toolState.Outline.color = config.ThemeColor;
                toolState.Outline.enabled = true;

                foreach(var graphic in toolState.ToolColorTinted) {
                    graphic.color = config.ThemeColor;
                }

                if(toolState.DetailCamera != null) {
                    toolState.DetailCamera.backgroundColor = config.SecondThemeColor;
                }
            } else {
                toolState.Outline.enabled = false;
            }

            toolState.CurrentTool = tool;
            toolState.CurrentToolDef = TabletToolDefinitions.Get(tool);

            toolState.CurrentToolDef.OnOpen?.Invoke(ctrl);

            if (highlights.HighlightedObject) {
                toolState.CurrentToolDef.OnHighlighted?.Invoke(highlights.HighlightedObject, ctrl);
            }

            toolState.Reticle.SetVisible(toolState.CurrentToolDef.ShowReticle);

            if (playFeedback) {
                TabletUtility.PlaySfx("Tablet.ModeChanged");
                TabletUtility.PlayHaptics(0.15f, 0.01f);
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