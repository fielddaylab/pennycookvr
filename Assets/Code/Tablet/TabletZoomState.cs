using System;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Variants;
using FieldDay;
using FieldDay.Audio;
using FieldDay.HID.XR;
using FieldDay.Scripting;
using FieldDay.SharedState;
using UnityEngine;

namespace Pennycook.Tablet {
    public class TabletZoomState : SharedStateComponent, IRegistrationCallbacks {
        static public readonly TableKeyPair Var_CurrentZoomLevel = TableKeyPair.Parse("tablet:zoomLevel");
        static public readonly TableKeyPair Var_CurrentZoomMultiplier = TableKeyPair.Parse("tablet:zoomMultiplier");

        public Camera ZoomCamera;

        public float[] ZoomLevels;
        public InvertedLabelDisplay[] ZoomLabels;
        [AudioEventRef] public StringHash32[] ZoomSfx;

        [NonSerialized] public float OriginalFOV;
        [NonSerialized] public int ZoomIndex;
        [NonSerialized] public float ZoomMultiplier;

        void IRegistrationCallbacks.OnDeregister() {
            ScriptUtility.UnbindVariable(Var_CurrentZoomLevel);
            ScriptUtility.UnbindVariable(Var_CurrentZoomMultiplier);
        }

        void IRegistrationCallbacks.OnRegister() {
            OriginalFOV = ZoomCamera.fieldOfView;
            Assert.True(ZoomLabels.Length == ZoomLevels.Length);

            ZoomLabels[0].SetState(true);
            ZoomMultiplier = 1;

            ScriptUtility.BindVariable(Var_CurrentZoomLevel, () => ZoomIndex);
            ScriptUtility.BindVariable(Var_CurrentZoomMultiplier, () => ZoomMultiplier);
        }
    }

    static public partial class TabletUtility {
        static public void AdjustZoom(TabletZoomState zoomState, int index, bool playFeedback) {
            int prevIdx = zoomState.ZoomIndex;
            if (prevIdx == index) {
                return;
            }
            
            if (prevIdx >= 0) {
                zoomState.ZoomLabels[prevIdx].SetState(false);
            }

            zoomState.ZoomIndex = index;

            float zoom = 1;

            if (index >= 0) {
                zoomState.ZoomLabels[index].SetState(true);
                zoom = zoomState.ZoomLevels[index];
            }

            zoomState.ZoomCamera.fieldOfView = zoomState.OriginalFOV / zoom;
            zoomState.ZoomMultiplier = zoom;

            if (playFeedback && index >= 0) {
                Sfx.Play(zoomState.ZoomSfx[index], Find.State<TabletControlState>().AudioLocation);
                using(var t = TempVarTable.Alloc()) {
                    t.Set("zoomLevel", index);
                    t.Set("zoomMultiplier", zoom);
                    ScriptUtility.Trigger(GameTriggers.ChangedTabletZoom, t);
                }

                PlayerHaptics.Play(XRHandIndex.Right, 0.1f, 0.03f);
            }
        }
    }
}