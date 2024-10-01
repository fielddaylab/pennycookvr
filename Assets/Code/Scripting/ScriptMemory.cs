using BeauUtil.Variants;
using FieldDay;
using FieldDay.Scripting;

namespace Pennycook {
    static public class ScriptMemory {
        static public VariantTable Global { get; private set; }
        static public VariantTable Margo { get; private set; }
        static public VariantTable Chapter { get; private set; }
        static public VariantTable Session { get; private set; }

        [InvokeOnBoot]
        static private void Initialize() {
            ScriptUtility.BindTable("global", (Global = new VariantTable("global")));
            ScriptUtility.BindTable("margo", (Margo = new VariantTable("margo")));
            ScriptUtility.BindTable("chapter", (Chapter = new VariantTable("chapter")));
            ScriptUtility.BindTable("session", (Session = new VariantTable("session")));
        }
    }
}