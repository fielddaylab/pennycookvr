using System;
using System.Collections;
using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Tags;
using BeauUtil.Variants;
using FieldDay.SharedState;
using Leaf;
using Leaf.Defaults;
using Leaf.Runtime;
using UnityEngine;

namespace FieldDay.Scripting {
    public abstract class ScriptPlugin : DefaultLeafManager<ScriptNode> {
        private readonly ScriptRuntimeState m_RuntimeState;
        private readonly ScriptDatabase m_Database;

        public ScriptPlugin(ScriptRuntimeState runtimeState, ScriptDatabase database, CustomVariantResolver inResolver, IMethodCache inCache = null, LeafRuntimeConfiguration inConfiguration = null)
            : base(GameLoop.Host, inResolver, inCache, inConfiguration) {
            m_RuntimeState = runtimeState;
            m_Database = database;
        }

        public override void OnEnd(LeafThreadState<ScriptNode> inThreadState) {
            LeafThreadHandle handle = inThreadState.GetHandle();

            if (m_RuntimeState.Cutscene == handle) {
                m_RuntimeState.Cutscene = default;
            }

            // TODO: clean up active thread set

            base.OnEnd(inThreadState);
        }

        #region Lookups

        public override bool TryLookupObject(StringHash32 inObjectId, LeafThreadState inThreadState, out object outObject) {
            bool result = m_RuntimeState.Actors.NamedActors.TryGetValue(inObjectId, out ILeafActor actor);
            outObject = actor;
            return result;
        }

        public override bool TryLookupLine(StringHash32 inLineCode, LeafNode inLocalNode, out string outLine) {
            // TODO: if non-default language, lookup from localization instead
            return inLocalNode.Package().TryGetLine(inLineCode, out outLine);
        }

        public override bool TryLookupNode(StringHash32 inNodeId, ScriptNode inLocalNode, out ScriptNode outLeafNode) {
            return ScriptDatabaseUtility.TryLookupNode(m_Database, inLocalNode, inNodeId, out outLeafNode);
        }

        #endregion // Lookups
    }
}