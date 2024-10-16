using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.Components;
using FieldDay.Scripting;
using UnityEngine;

namespace Pennycook.Tablet {
    [RequireComponent(typeof(TabletHighlightable))]
    public sealed class TabletCountable : BatchedComponent {
        public TabletCountingGroup Group;

        
    }

    static public class TabletCountUtility {
        static public bool IsCountable(TabletCountable countable) {
            var group = countable.Group;
            if (group.State == TabletCountingGroupState.Inactive || group.State == TabletCountingGroupState.Completed) {
                return false;
            }
            if (group.CurrentlyCounted.Contains(countable)) {
                return false;
            }
            return true;
        }

        static public bool TryCount(TabletHighlightable highlightable, TabletCountable countable, double currentTime) {
            if (IsCountable(countable)) {
                /*interactable.CooldownTimestamp = currentTime + interactable.InteractionCooldown;
                interactable.OnInteract.Invoke(new TabletInteractionArgs() {
                    Interactable = interactable
                });*/

                countable.Group.CurrentlyCounted.Add(countable);

                //bool identified = Ref.Replace(ref highlightable.Identified, true);
                //if (identified) {
                    TabletUtility.UpdateHighlightLabels(Find.State<TabletHighlightState>(), TabletUtility.GetLabelsForHighlightable(highlightable));
                    TabletUtility.PlayHaptics(0.3f, 0.08f);
                    TabletUtility.PlaySfx("Tablet.Identified");
                //}

                if(countable.Group.IsCountFinished()) {
                    var actor = ScriptUtility.Actor(countable.Group);
                    if (actor != null) {
                        using (var table = TempVarTable.Alloc()) {
                            table.ActorInfo(actor);
                            ScriptUtility.Trigger(TabletTriggers.TabletCounted, table);
                        }
                    }
                }

                return true;
            }

            return false;
        }
    }
}