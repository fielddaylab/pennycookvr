using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Assets;
using Leaf.Runtime;

namespace FieldDay.Scripting {
    /// <summary>
    /// Set of all registered actors and a map of all named registered actors.
    /// </summary>
    public class ScriptActorMap {
        public readonly HashSet<ILeafActor> AllActors;
        public readonly Dictionary<StringHash32, ILeafActor> NamedActors;

        public ScriptActorMap(int capacity) {
            AllActors = new HashSet<ILeafActor>(capacity);
            NamedActors = MapUtils.Create<StringHash32, ILeafActor>(capacity);
        }

        /// <summary>
        /// Registers an actor.
        /// Returns if the actor was successfully registered.
        /// </summary>
        public bool Register(ILeafActor actor) {
            if (!AllActors.Add(actor)) {
                return false;
            }

            StringHash32 id = actor.Id;
            if (!id.IsEmpty) {
                if (NamedActors.TryGetValue(id, out ILeafActor existing)) {
                    Log.Error("[ScriptActorMap] Duplicate actor ids - another actor with id '{0}' registered", id.ToDebugString());
                } else {
                    NamedActors.Add(id, actor);
                    Log.Debug("[ScriptActorMap] Registered actor with id '{0}'", id.ToDebugString());
                }
            } else {
                Log.Debug("[ScriptActorMap] Registered unnamed actor with GameObject name '{0}'", AssetUtility.NameOf(actor));
            }
            return true;
        }

        /// <summary>
        /// Deregisters an actor.
        /// Returns if the actor was successfully removed.
        /// </summary>
        public bool Deregister(ILeafActor actor) {
            if (!AllActors.Remove(actor)) {
                return false;
            }

            StringHash32 id = actor.Id;
            if (!id.IsEmpty) {
                if (NamedActors.TryGetValue(id, out ILeafActor existing) && existing == actor) {
                    NamedActors.Remove(id);
                    Log.Debug("[ScriptActorMap] Deregistered actor with id '{0}'", id.ToDebugString());
                }
            } else {
                Log.Debug("[ScriptActorMap] Deregistered unnamed actor with GameObject name '{0}'", AssetUtility.NameOf(actor));
            }
            return true;
        }
    }
}