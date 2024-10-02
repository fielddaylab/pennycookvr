using System;
using System.Collections.Generic;
using FieldDay.Scripting;

namespace Pennycook.Tablet {
    public sealed class TabletCountingGroup : ScriptActorComponent {
        [NonSerialized] public int TotalInGroup;
        [NonSerialized] public HashSet<TabletCountable> CurrentlyCounted;

        public override void OnScriptRegister(ScriptActor actor) {
            base.OnScriptRegister(actor);

            CurrentlyCounted = new HashSet<TabletCountable>(64);
        }
    }
}