using BeauUtil;
using FieldDay;
using FieldDay.Scripting;
using FieldDay.Sockets;
using FieldDay.VRHands;
using Pennycook.Tablet;

namespace Pennycook {
    static public class TabletTriggers {
        static public readonly StringHash32 LiftedTablet = "TabletLifted";
        static public readonly StringHash32 DroppedTablet = "TabletDropped";
        static public readonly StringHash32 ChangedTabletTool = "ChangedTabletTool";
        static public readonly StringHash32 ChangedTabletZoom = "ChangedTabletZoom";

        static public readonly StringHash32 TabletInteracted = "TabletInteracted";
        static public readonly StringHash32 TabletIdentified = "TabletIdentified";

        static public readonly StringHash32 TabletCounted = "TabletCounted";

        static public readonly StringHash32 ObjectHighlighted = "TabletHighlighted";
        static public readonly StringHash32 ObjectUnhighlighted = "TabletUnhighlighted";

        [InvokeOnBoot]
        static private void Initialize() {
            VRGame.Events.Register<TabletHighlightable>(GameEvents.ObjectHighlighted, OnObjectHighlight)
                .Register<TabletHighlightable>(GameEvents.ObjectUnhighlighted, OnObjectUnhighlight);
        }

        static private void OnObjectHighlight(TabletHighlightable highlightable) {
            using(var table = TempVarTable.Alloc()) {
                table.ActorInfo(ScriptUtility.Actor(highlightable));
                ScriptUtility.Trigger(ObjectHighlighted, table);
            }
        }

        static private void OnObjectUnhighlight(TabletHighlightable highlightable) {
            using (var table = TempVarTable.Alloc()) {
                table.ActorInfo(ScriptUtility.Actor(highlightable));
                ScriptUtility.Trigger(ObjectUnhighlighted, table);
            }
        }
    }
}