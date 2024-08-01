using FieldDay;
using FieldDay.Systems;

namespace Pennycook.Tablet {
    [SysUpdate(GameLoopPhase.Update, -10)]
    public class TabletInteractionCleanupSystem : ComponentSystemBehaviour<TabletInteractable> {
        public override bool HasWork() {
            return base.HasWork() && Frame.Interval(16);
        }

        public override void ProcessWorkForComponent(TabletInteractable component, float deltaTime) {
            TabletInteractionUtility.CleanBlockingTasks(component);
        }
    }
}