using System;
using System.Collections.Generic;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Variants;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.Scripting;
using FieldDay.SharedState;
using FieldDay.UI;
using Leaf.Runtime;
using TMPro;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;

namespace Pennycook.Tablet {
    public class TabletHighlightState : SharedStateComponent, IRegistrationCallbacks {
        static public readonly TableKeyPair Var_CurrentHighlightId = TableKeyPair.Parse("tablet:highlightedId");
        static public readonly TableKeyPair Var_CurrentHighlightType = TableKeyPair.Parse("tablet:highlightedType");

        public List<StringHash32> ActiveGoalIds = new List<StringHash32>();
        public BitSet32 GoalsComplete = new BitSet32();


        public Camera LookCamera;

        [Header("Selection Box")]
        public RectTransform HighlightBox;
        public CanvasGroup HighlightBoxGroup;

        [Header("Details")]
        public GameObject DetailsGroup;
        public GameObject GoalsGroup;
        public TabletCheckboxItem[] GoalItems;
        public TMP_Text DetailsHeader;
        public TMP_Text DetailsText;

        [Header("Raycast Configuration")]
        public float RaycastSize = 0.4f;
        public float RaycastMinDistance = 1;

        [NonSerialized] public Transform CachedLookCameraTransform;
        [NonSerialized] public Vector2 CachedHighlightCornerScale;

        [NonSerialized] public RaycastJob RaycastJob;
        [NonSerialized] public TabletHighlightable HighlightedObject;
        [NonSerialized] public Rect TargetHighlightCorners;
        [NonSerialized] public Routine BoxTransitionRoutine;
        [NonSerialized] public bool IsBoxVisible;

        void IRegistrationCallbacks.OnDeregister() {
            ScriptUtility.UnbindVariable(Var_CurrentHighlightId);
            ScriptUtility.UnbindVariable(Var_CurrentHighlightType);
        }

        void IRegistrationCallbacks.OnRegister() {
            LookCamera.CacheComponent(ref CachedLookCameraTransform);
            CachedHighlightCornerScale = ((RectTransform) HighlightBox.parent).rect.size;

            ScriptUtility.BindVariable(Var_CurrentHighlightId, () => ScriptUtility.ActorId(HighlightedObject));
            ScriptUtility.BindVariable(Var_CurrentHighlightType, () => ScriptUtility.ActorType(HighlightedObject));

            Log.Msg("[TabletHighlightState] Parent size is {0}", CachedHighlightCornerScale);
        }
    }

    public struct TabletGoal {
        public bool Completed;
        public StringHash32 Id;
        public string Text;
    }

    static public partial class TabletUtility {

        [SharedStateReference]
        static public TabletHighlightState HighlightState { get; private set; }

        static private readonly RaycastHit[] s_RaycastHitBuffer = new RaycastHit[32];

        // iteration count for spherecast
        private const int IterationCount = 8;

        // uncomment the default and solid masks to allow for other objects to occlude the target type
        public const int DefaultSearchMask = /* LayerMasks.Default_Mask | LayerMasks.Solid_Mask | */ LayerMasks.Grabbable_Mask | LayerMasks.Highlightable_Mask;
        public const int TravelSearchMask = /* LayerMasks.Default_Mask | LayerMasks.Solid_Mask | */ LayerMasks.Warpable_Mask;

        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [Il2CppSetOption(Option.NullChecks, false)]
        static public unsafe Rect CalculateViewportAlignedBoundingBox(Bounds bounds, Camera referenceCamera, Vector2 scale) {
            Vector3* corners = stackalloc Vector3[8];
            Vector3 min = bounds.min, max = bounds.max;
            corners[0] = min;
            corners[1] = new Vector3(min.x, min.y, max.z);
            corners[2] = new Vector3(min.x, max.y, min.z);
            corners[3] = new Vector3(min.x, max.y, max.z);
            corners[4] = new Vector3(max.x, min.y, min.z);
            corners[5] = new Vector3(max.x, min.y, max.z);
            corners[6] = new Vector3(max.x, max.y, min.z);
            corners[7] = max;

            //DebugDraw.AddBounds(bounds, Color.blue.WithAlpha(0.2f), 0.2f, 0.1f);

			Vector2* viewCorners = stackalloc Vector2[8];
            for (int i = 0; i < 8; i++) {
                viewCorners[i] = ClampTo01Space(referenceCamera.WorldToViewportPoint(corners[i], Camera.MonoOrStereoscopicEye.Mono));
            }

            Rect r = Geom.MinRect(new UnsafeSpan<Vector2>(viewCorners, 8));
            r.x *= scale.x;
            r.y *= scale.y;
            r.width *= scale.x;
            r.height *= scale.y;
            return r;
        }
		
		static private Vector2 ClampTo01Space(Vector3 input) {
			Vector2 output;
			output.x = Mathf.Clamp01(input.x);
			output.y = Mathf.Clamp01(input.y);
			return output;
		}

        [LeafMember("IsTabletHighlighted")]
        static private bool LeafIsHighlighted(ScriptActor actor) {
            if (actor == null) {
                return false;
            }

            if (!actor.TryGetComponent(out TabletHighlightable h)) {
                Log.Warn("IsTabletHighlighted(): Actor '{0}' is not highlightable", actor);
                return false;
            }
            
            return Find.State<TabletHighlightState>().HighlightedObject == h;
        }

        [LeafMember("CreateGoal")]
        static private void LeafCreateGoal(StringHash32 id, string text) {
            int index = HighlightState.ActiveGoalIds.Count - 1;
            HighlightState.ActiveGoalIds.Add(id);
            HighlightState.GoalItems[index].Text.SetTextAndActive(text);
            HighlightState.GoalItems[index].Check.SetAlpha(0);
            HighlightState.GoalsComplete.Unset(index);
        }

        [LeafMember("SetGoalComplete")]
        static private bool LeafSetGoalComplete(StringHash32 id, bool complete) {
            int index = HighlightState.ActiveGoalIds.IndexOf(id);
            return SetGoalComplete(index, complete);
        }

        static public bool SetGoalComplete(int index, bool complete) {
            if (index < 0 || index >= HighlightState.GoalItems.Length) return false;
            HighlightState.GoalsComplete.Set(index);
            HighlightState.GoalItems[index].Check.SetAlpha(complete ? 1 : 0);
            return true;
        }

        [LeafMember("IsGoalComplete")]
        static private bool LeafCheckGoalComplete(StringHash32 id) {
            return HighlightState.GoalsComplete[HighlightState.ActiveGoalIds.IndexOf(id)];
        }

        [LeafMember("ClearGoals")]
        static private void LeafClearGoals() {
            HighlightState.ActiveGoalIds.Clear();
            HighlightState.GoalsComplete.Clear();
            for (int i = 0; i <  HighlightState.GoalItems.Length; i++) {
                HighlightState.GoalItems[i].Text.SetTextAndActive("");
                HighlightState.GoalItems[i].Check.SetAlpha(0);
            }
        }

        static public void UpdateHighlightLabels(TabletHighlightState highlight, in TabletDetailsContent contents) {
            TMPUtility.SetTextAndActive(highlight.DetailsHeader, contents.DetailedHeader);
            TMPUtility.SetTextAndActive(highlight.DetailsText, contents.DetailedText);
            highlight.DetailsGroup.SetActive(true);
            highlight.GoalsGroup.SetActive(false);
        }

        static public TabletDetailsContent GetLabelsForHighlightable(TabletHighlightable highlightable) {
            if (!highlightable.Identified) {
                return highlightable.UnidentifiedContents;
            }
            return highlightable.Contents;
        }
    }
}