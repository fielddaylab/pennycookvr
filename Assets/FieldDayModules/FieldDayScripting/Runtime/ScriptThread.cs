using System;
using BeauPools;
using BeauUtil;
using Leaf;
using Leaf.Runtime;
using FieldDay.VO;

namespace FieldDay.Scripting {
    /// <summary>
    /// Scripting thread implementation.
    /// </summary>
    public class ScriptThread : LeafThreadState<ScriptNode> {
        private readonly IPool<ScriptThread> m_Pool;

        private StringHash32 m_OriginalNodeId;
        private ScriptNodePriority m_Priority;
        private ScriptThreadFlags m_Flags;

        private VOPlaybackHandle m_Voiceover;

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

        public bool IsSkipping() {
            return (m_Flags & ScriptThreadFlags.Skipping) != 0;
        }

        protected override void Reset() {
            base.Reset();

            m_Flags = default;
            m_OriginalNodeId = null;
            m_Priority = default;
            m_Pool.Free(this);
        }
    }

    [Flags]
    internal enum ScriptThreadFlags {
        None = 0,

        Skipping = 0x01,
        Cutscene = 0x02,

    }
}