using BeauPools;
using BeauUtil;
using Leaf;
using Leaf.Runtime;

namespace FieldDay.Scripting {
    /// <summary>
    /// Scripting thread implementation.
    /// </summary>
    public class ScriptThread : LeafThreadState<ScriptNode> {
        private readonly IPool<ScriptThread> m_Pool;

        private StringHash32 m_OriginalNodeId;
        private ScriptNodePriority m_Priority;

        public ScriptThread(IPool<ScriptThread> pool, ILeafPlugin<ScriptNode> inPlugin) : base(inPlugin) {
            m_Pool = pool;
        }

        public StringHash32 InitialNodeId() {
            return m_OriginalNodeId;
        }

        public ScriptNodePriority Priority() {
            return m_Priority;
        }

        public void SetInitialNode(ScriptNode node) {
            m_OriginalNodeId = node.Id();
            m_Priority = node.Priority;
        }

        protected override void Reset() {
            base.Reset();

            m_OriginalNodeId = null;
            m_Priority = default;
            m_Pool.Free(this);
        }
    }
}