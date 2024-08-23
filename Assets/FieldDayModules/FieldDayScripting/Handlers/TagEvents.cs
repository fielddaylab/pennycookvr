using BeauUtil;
using BeauUtil.Tags;
using Leaf;

namespace FieldDay.Scripting {
    static public class TagEvents {
        static public readonly StringHash32 ConfigureVoxOverlap = "vox-overlap";

        static internal void ConfigureParsers(CustomTagParserConfig parser, ILeafPlugin plugin) {
            LeafUtils.ConfigureDefaultParsers(parser, plugin, null);

            parser.AddEvent("vox-overlap", ConfigureVoxOverlap).WithFloatData(-0.2f);
        }

        static internal void ConfigureHandlers(TagStringEventHandler handler, ILeafPlugin plugin) {
            LeafUtils.ConfigureDefaultHandlers(handler, plugin);

            handler.Register(ConfigureVoxOverlap, Event_VoxOverlap);
            handler.Register(LeafUtils.Events.Character, (e, o) => { });
            handler.Register(LeafUtils.Events.Pose, (e, o) => { });
        }


        static private void Event_VoxOverlap(TagEventData evt, object context) {
            var thread = (ScriptThread) context;
            thread.SetVoxReleaseTime(evt.GetFloat());
        }
    }
}