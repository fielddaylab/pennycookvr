using System;
using BeauPools;
using BeauUtil;
using Leaf;
using Leaf.Runtime;
using FieldDay.Vox;
using BeauUtil.Debugger;
using FieldDay.Audio;

namespace FieldDay.Scripting {
    /// <summary>
    /// Scripting thread implementation.
    /// </summary>
    public class ScriptThread : LeafThreadState<ScriptNode> {
        private readonly IPool<ScriptThread> m_Pool;
        private readonly ScriptPlugin m_CustomPlugin;

        private StringHash32 m_OriginalNodeId;
        private StringHash32 m_Target;
        private ScriptNodePriority m_Priority;
        private ScriptThreadFlags m_Flags;

        private int m_CutsceneDepth;
        private AudioHandle m_Voiceover;

        public ScriptThread(IPool<ScriptThread> pool, ScriptPlugin inPlugin) : base(inPlugin) {
            m_Pool = pool;
            m_CustomPlugin = inPlugin;
        }

        #region Initial State

        public StringHash32 InitialNodeId() {
            return m_OriginalNodeId;
        }

        public StringHash32 Target() {
            return m_Target;
        }

        public ScriptNodePriority Priority() {
            return m_Priority;
        }

        internal void SetInitialNode(ScriptNode node, StringHash32 target) {
            m_OriginalNodeId = node.Id();
            m_Target = target;
            m_Priority = node.Priority;
        }

        #endregion // Initial State

        #region Cutscene

        internal void PushCutscene() {
            if (m_CutsceneDepth++ == 0) {
                m_CustomPlugin.SetCutscene(GetHandle());
            }
        }

        internal void PopCutscene() {
            Assert.True(m_CutsceneDepth > 0, "ScriptThread.Push/PopCutscene calls unbalanced");
            if (m_CutsceneDepth-- == 1) {
                m_CustomPlugin.DereferenceCutscene(GetHandle());
            }
        }

        #endregion // Cutscene

        #region Skipping

        public bool IsSkipping() {
            return (m_Flags & ScriptThreadFlags.Skipping) != 0;
        }

        internal void StopSkipping() {
            // TODO: Implement
        }

        #endregion // Skipping

        protected override void Reset() {
            m_CustomPlugin.StopTracking(this);
            
            base.Reset();

            while(m_CutsceneDepth > 0) {
                PopCutscene();
            }

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