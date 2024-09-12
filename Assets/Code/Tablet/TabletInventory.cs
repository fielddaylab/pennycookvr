using System.Collections.Generic;
using BeauUtil;
using FieldDay;
using FieldDay.SharedState;

namespace Pennycook.Tablet {
    public class TabletInventory : SharedStateComponent {
        public HashSet<StringHash32> ObservedBehaviors = SetUtils.Create<StringHash32>(32);
    }
}