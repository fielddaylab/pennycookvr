using BeauUtil;
using BeauUtil.Blocks;
using BeauUtil.Debugger;
using BeauUtil.IO;
using Leaf;
using Leaf.Compiler;

namespace FieldDay.Scripting {
    public class ScriptNodePackage : LeafNodePackage<ScriptNode> {
        private bool m_Active;
        private int m_UseCount = 0;
        private IHotReloadable m_HotReload;

        public ScriptNodePackage(string name) : base(name) {
        }

        #region Active State

        /// <summary>
        /// Returns if the package is loaded and active.
        /// </summary>
        public bool IsActive() {
            return m_Active;
        }

        /// <summary>
        /// Sets whether nodes from this package can be triggered. 
        /// </summary>
        public bool SetActive(bool active) {
            return Ref.Replace(ref m_Active, active);
        }

        #endregion // Active State

        #region Reference Count

        /// <summary>
        /// Adds a reference. This tracks how many script stack frames are currently using this package.
        /// </summary>
        public void AddReference() {
            m_UseCount++;
        }

        /// <summary>
        /// Removes a reference. This tracks how many script stack frames are currently using this package.
        /// </summary>
        public void ReleaseReference() {
            Assert.True(m_UseCount > 0, "Unbalanced AddReference/ReleaseReference calls");
            m_UseCount--;
        }

        /// <summary>
        /// Returns if any script stack frames are currently using this package.
        /// </summary>
        public bool IsReferenced() {
            return m_UseCount > 0;
        }

        #endregion // Reference Count

        #region Hot Reload

        // TODO: Implement

        #endregion // Hot Reload

        #region Generator

        /// <summary>
        /// Package parser.
        /// </summary>
        public sealed class Parser : LeafParser<ScriptNode, ScriptNodePackage> {
            static public readonly Parser Instance = new Parser();

            public override ScriptNodePackage CreatePackage(string inFileName) {
                return new ScriptNodePackage(inFileName);
            }

            protected override ScriptNode CreateNode(string inFullId, StringSlice inExtraData, ScriptNodePackage inPackage) {
                return new ScriptNode(inFullId, inPackage);
            }

            public override void OnEnd(IBlockParserUtil inUtil, ScriptNodePackage inPackage, bool inbError) {
                base.OnEnd(inUtil, inPackage, inbError);

                if (inbError) {
                    Log.Error("[ScriptNodePackage] Package '{0}' failed to compile", inPackage.Name());
                }
            }
        }

        #endregion // Generator
    }
}