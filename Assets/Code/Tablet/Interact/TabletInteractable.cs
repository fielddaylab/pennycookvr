using System;
using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.Components;
using FieldDay.Scripting;
using UnityEngine;

namespace Pennycook.Tablet {
    [RequireComponent(typeof(TabletHighlightable))]
    public class TabletInteractable : BatchedComponent {
        public bool CanInteract = true;
        public float InteractionCooldown;
        public TabletInteractableVerb Verb = TabletInteractableVerb.Scan;

        [NonSerialized] public double CooldownTimestamp;

        public readonly CastableEvent<TabletInteractionArgs> OnInteract = new CastableEvent<TabletInteractionArgs>(1);
        public readonly RingBuffer<Routine> BlockingTasks = new RingBuffer<Routine>(4, RingBufferMode.Expand);
    }

    public enum TabletInteractableVerb {
        None,
        Scan,
        Identify,
        Interact
    }

    public struct TabletInteractionArgs {
        public TabletInteractable Interactable;

        public void AddCooldown(float cooldown) {
            Interactable.CooldownTimestamp += cooldown;
        }

        public void AddBlockingTask(Routine routine) {
            Interactable.BlockingTasks.PushBack(routine);
        }
    }

    static public class TabletInteractionUtility {
        static public void CleanBlockingTasks(TabletInteractable interactable) {
            while(interactable.BlockingTasks.TryPeekFront(out Routine task) && !task) {
                interactable.BlockingTasks.PopFront();
            }
        }

        static public bool CanInteract(TabletInteractable interactable, double currentTime) {
            if (interactable.CanInteract && interactable.CooldownTimestamp <= currentTime) {
                CleanBlockingTasks(interactable);
                return interactable.BlockingTasks.Count == 0;
            }
            return false;
        }

        static public bool HasInteractions(TabletHighlightable highlightable, TabletInteractable interactable) {
            if (interactable.OnInteract.Count > 0) {
                return true;
            }
            if (interactable.Verb == TabletInteractableVerb.Identify) {
                return !highlightable.Identified;
            }
            return interactable.Verb != TabletInteractableVerb.None;
        }

        static public bool TryInteract(TabletHighlightable highlightable, TabletInteractable interactable, double currentTime) {
            if (CanInteract(interactable, currentTime)) {
                interactable.CooldownTimestamp = currentTime + interactable.InteractionCooldown;
                interactable.OnInteract.Invoke(new TabletInteractionArgs() {
                    Interactable = interactable
                });

                bool identified = Ref.Replace(ref highlightable.Identified, true);
                if (identified) {
                    TabletUtility.UpdateHighlightLabels(Find.State<TabletHighlightState>(), TabletUtility.GetLabelsForHighlightable(highlightable));
                }

                var actor = ScriptUtility.Actor(interactable);
                if (actor != null) {
                    using (var table = TempVarTable.Alloc()) {
                        table.ActorInfo(actor);
                        ScriptUtility.Trigger(TabletTriggers.TabletInteracted, table);

                        if (identified) {
                            ScriptUtility.Trigger(TabletTriggers.TabletIdentified, table);
                        }
                    }
                }
                return true;
            }

            return false;
        }
    }
}