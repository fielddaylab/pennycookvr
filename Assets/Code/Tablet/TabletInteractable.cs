using System;
using BeauRoutine;
using BeauUtil;
using FieldDay.Components;
using UnityEngine;

namespace Pennycook.Tablet {
    [RequireComponent(typeof(TabletHighlightable))]
    public class TabletInteractable : BatchedComponent {
        public bool CanInteract = true;
        public float InteractionCooldown;

        [NonSerialized] public double CooldownTimestamp;

        public readonly CastableEvent<TabletInteractionArgs> OnInteract = new CastableEvent<TabletInteractionArgs>(1);
        public readonly RingBuffer<Routine> BlockingTasks = new RingBuffer<Routine>(4, RingBufferMode.Expand);
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

        static public bool HasInteractions(TabletInteractable interactable) {
            return interactable.OnInteract.Count > 0;
        }

        static public bool TryInteract(TabletInteractable interactable, double currentTime) {
            if (CanInteract(interactable, currentTime)) {
                interactable.CooldownTimestamp = currentTime + interactable.InteractionCooldown;
                interactable.OnInteract.Invoke(new TabletInteractionArgs() {
                    Interactable = interactable
                });
                return true;
            }

            return false;
        }
    }
}