using System;
using System.Runtime.InteropServices;
using BeauUtil;
using BeauUtil.Blocks;
using BeauUtil.Debugger;
using Leaf;
using Leaf.Runtime;

namespace FieldDay.Scripting {
    /// <summary>
    /// Scripting node.
    /// </summary>
    public class ScriptNode : LeafNode {
        public readonly string FullName;
        public ScriptNodeFlags Flags;

        public StringHash32 TriggerOrFunctionId;
        public int SortingScore;

        public LeafExpressionGroup Conditions;
        public StringHash32 TargetId;
        public ScriptNodePriority Priority = ScriptNodePriority.Medium;
        public ScriptNodeCooldown RepeatPeriod;
        public ScriptNodeMemoryTarget PersistenceType;
        public float SelectionWeight = 1;
        [BlockMeta("tag")] public StringHash32 Tag;

        public ScriptNode(string fullName, ScriptNodePackage package) : base(fullName, package) {
            FullName = fullName;
        }

        public new ScriptNodePackage Package() { return (ScriptNodePackage) m_Package; }

        #region Internal

        [BlockMeta("trigger")]
        private void SetTrigger(StringHash32 triggerId) {
            if ((Flags & ScriptNodeFlags.Function) != 0) {
                Log.Warn("[ScriptNode] 'trigger' is not compatible with function node '{0}'", FullName);
            } else {
                Flags |= ScriptNodeFlags.Trigger;
                TriggerOrFunctionId = triggerId;
            }
        }

        [BlockMeta("function")]
        private void SetFunction(StringHash32 triggerId) {
            if ((Flags & ScriptNodeFlags.Trigger) != 0) {
                Log.Warn("[ScriptNode] 'function' is not compatible with trigger node '{0}'", FullName);
            } else {
                Flags |= ScriptNodeFlags.Function;
                TriggerOrFunctionId = triggerId;
            }
        }

        [BlockMeta("who")]
        private void SetTarget(StringHash32 targetId) {
            if ((Flags & (ScriptNodeFlags.Trigger | ScriptNodeFlags.Function)) == 0) {
                Log.Warn("[ScriptNode] 'who' is not compatible with non-function, non-trigger node '{0}'", FullName);
            } else {
                TargetId = targetId;
                if (targetId == AnyTarget) {
                    Flags |= ScriptNodeFlags.AnyTarget;
                }
            }
        }

        [BlockMeta("when")]
        private void SetConditions(StringSlice conditions) {
            if ((Flags & (ScriptNodeFlags.Trigger | ScriptNodeFlags.Function)) == 0) {
                Log.Warn("[ScriptNode] 'when' is not compatible with non-function, non-trigger node '{0}'", FullName);
            } else {
                Conditions = LeafUtils.CompileExpressionGroup(this, conditions);
                SortingScore += Conditions.Count;
            }
        }

        [BlockMeta("evalPriority")]
        private void AdjustEvalPriority(int priority) {
            if ((Flags & ScriptNodeFlags.Trigger) == 0) {
                Log.Warn("[ScriptNode] 'evalPriority' is not compatible with non-trigger node '{0}'", FullName);
            } else {
                SortingScore += priority;
            }
        }

        [BlockMeta("interrupt")]
        private void SetAsInterrupt() {
            if ((Flags & ScriptNodeFlags.Trigger) == 0) {
                Log.Warn("[ScriptNode] 'interrupt' is not compatible with non-trigger node '{0}'", FullName);
            } else {
                Flags |= ScriptNodeFlags.InterruptSamePriority;
            }
        }

        [BlockMeta("cutscene")]
        private void SetAsCutscene() {
            if ((Flags & ScriptNodeFlags.Function) != 0) {
                Log.Warn("[ScriptNode] 'cutscene' is not compatible with function node '{0}'", FullName);
            } else {
                Flags |= ScriptNodeFlags.Cutscene;
                Priority = ScriptNodePriority.Cutscene;
            }
        }

        [BlockMeta("ignoreDuringCutscene")]
        private void SetIgnoreDuringCutscene() {
            if ((Flags & (ScriptNodeFlags.Trigger | ScriptNodeFlags.Function)) == 0) {
                Log.Warn("[ScriptNode] 'ignoreDuringCutscene' is not compatible with non-function, non-trigger node '{0}'", FullName);
            } else {
                Flags |= ScriptNodeFlags.IgnoreDuringCutscene;
            }
        }

        [BlockMeta("repeat")]
        private void SetRepeat(uint repeatPeriod) {
            if ((Flags & ScriptNodeFlags.Once) != 0) {
                Log.Warn("[ScriptNode] 'repeat' is not compatible with once node '{0}'", FullName);
            } else if ((Flags & ScriptNodeFlags.Trigger) == 0) {
                Log.Warn("[ScriptNode] 'repeat' is not compatible with non-trigger node '{0}'", FullName);
            } else {
                RepeatPeriod.NodeWindow = (int) repeatPeriod;
                Flags &= ~ScriptNodeFlags.HasTimeCooldown;
            }
        }

        [BlockMeta("cooldown")]
        private void SetCooldown(float cooldownSeconds) {
            if ((Flags & ScriptNodeFlags.Once) != 0) {
                Log.Warn("[ScriptNode] 'cooldown' is not compatible with once node '{0}'", FullName);
            } else if ((Flags & ScriptNodeFlags.Trigger) == 0) {
                Log.Warn("[ScriptNode] 'cooldown' is not compatible with non-trigger node '{0}'", FullName);
            } else {
                RepeatPeriod.TimeWindow = cooldownSeconds;
                Flags |= ScriptNodeFlags.HasTimeCooldown;
            }
        }

        [BlockMeta("once")]
        private void SetOnce(StringSlice type) {
            if (RepeatPeriod.NodeWindow > 0 || RepeatPeriod.TimeWindow > 0) {
                Log.Warn("[ScriptNode] 'once' is not compatible with repeat node '{0}'", FullName);
            } else if (type.Equals("untracked", true)) {
                Log.Warn("[ScriptNode] 'untracked' is an unsupported value for 'once' on node '{0}'", FullName);
            } else {
                Flags |= ScriptNodeFlags.Once;
                if (type.Equals("session", true)) {
                    PersistenceType = ScriptNodeMemoryTarget.Session;
                } else if (type.Equals("chapter", true)) {
                    PersistenceType = ScriptNodeMemoryTarget.Chapter;
                } else {
                    PersistenceType = ScriptNodeMemoryTarget.Persistent;
                }
            }
        }

        [BlockMeta("priority")]
        private void SetPriority(ScriptNodePriority priority) {
            if ((Flags & ScriptNodeFlags.Cutscene) != 0 && priority != ScriptNodePriority.Cutscene) {
                Log.Warn("[ScriptNode] 'priority' is not compatible with cutscene node '{0}'", FullName);
            } else if ((Flags & ScriptNodeFlags.Trigger) == 0) {
                Log.Warn("[ScriptNode] 'priority' is not compatible with non-trigger node '{0}'", FullName);
            } else {
                Priority = priority;
            }
        }

        [BlockMeta("exposed")]
        private void SetExposed() {
            Flags |= ScriptNodeFlags.Exposed;
        }

        [BlockMeta("weight")]
        private void SetSelectionWeight(float probability) {
            if ((Flags & ScriptNodeFlags.Trigger) == 0) {
                Log.Warn("[ScriptNode] 'weight' is not compatible with non-trigger node '{0}'", FullName);
            } else if (probability <= 0) {
                Log.Warn("[ScriptNode] 'weight' does not support values of 0 or less on node '{0}'", FullName);
            } else if (probability != SelectionWeight) {
                SelectionWeight = probability;
                Flags |= ScriptNodeFlags.IsWeighted;
            }
        }

        #endregion // Internal

        static public readonly StringHash32 AnyTarget = "*";
    }

    /// <summary>
    /// Script node behavior flags.
    /// </summary>
    [Flags]
    public enum ScriptNodeFlags : ulong {

        /// <summary>
        /// The node is a trigger.
        /// </summary>
        Trigger = 0x01,

        /// <summary>
        /// The node is a function.
        /// </summary>
        Function = 0x02,

        /// <summary>
        /// The node may only be visited once.
        /// </summary>
        Once = 0x04,

        /// <summary>
        /// The node is treated as a cutscene.
        /// </summary>
        Cutscene = 0x08,

        /// <summary>
        /// The node is exposed for manual lookups.
        /// </summary>
        Exposed = 0x10,

        /// <summary>
        /// The node cannot be visited during a cutscene.
        /// </summary>
        IgnoreDuringCutscene = 0x20,

        /// <summary>
        /// The node takes precedence over playing nodes of the same priority.
        /// </summary>
        InterruptSamePriority = 0x40,

        /// <summary>
        /// The node is valid for any target.
        /// </summary>
        AnyTarget = 0x80,

        /// <summary>
        /// The node will not wait for screen transitions to finish before running.
        /// </summary>
        DoNotWaitForScreenTransitions = 0x100,

        /// <summary>
        /// The node has a weighted probability. This affects node selection.
        /// </summary>
        IsWeighted = 0x200,

        /// <summary>
        /// The node has a timed cooldown.
        /// </summary>
        HasTimeCooldown = 0x400
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct ScriptNodeCooldown {
        [FieldOffset(0)] public int NodeWindow;
        [FieldOffset(0)] public float TimeWindow;
    }

    /// <summary>
    /// Script thread priority.
    /// </summary>
    public enum ScriptNodePriority : byte {
        None,
        Low,
        Medium,
        High,
        Cutscene
    }

    /// <summary>
    /// Persistence target for once and repeat.
    /// </summary>
    public enum ScriptNodeMemoryTarget : byte {
        Untracked,
        Chapter,
        Session,
        Persistent,
    }
}