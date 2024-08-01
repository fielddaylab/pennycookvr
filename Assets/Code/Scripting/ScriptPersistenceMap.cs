using System;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;

namespace FieldDay.Scripting {
    public sealed class ScriptPersistenceMap {
        public const int MaxMemoryTargets = 3;

        public struct TimestampedNodeRecord {
            public StringHash32 Id;
            public float Timestamp;
        }

        public RingBuffer<TimestampedNodeRecord> RecentlyViewedNodeIds = new RingBuffer<TimestampedNodeRecord>(32, RingBufferMode.Overwrite);
        public HashSet<StringHash32>[] VisitedNodesMap = new HashSet<StringHash32>[MaxMemoryTargets];

        public ScriptPersistenceMap(int visitedNodeCapacity) {
            for(int i = 0; i < MaxMemoryTargets; i++) {
                VisitedNodesMap[i] = SetUtils.Create<StringHash32>(visitedNodeCapacity);
            }
        }

        /// <summary>
        /// Returns if the given node has been recently seen in a specific window.
        /// </summary>
        public bool HasRecentlySeen(StringHash32 nodeId, int window) {
            window = Math.Min(window, RecentlyViewedNodeIds.Count);
            for(int i = 0; i < window; i++) {
                if (RecentlyViewedNodeIds[i].Id == nodeId) {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns if the given node has been recently seen in a specific time window.
        /// </summary>
        public bool HasRecentlySeenAfterTimestamp(StringHash32 nodeId, float minTimestamp) {
            for (int i = 0; i < RecentlyViewedNodeIds.Count; i++) {
                var pair = RecentlyViewedNodeIds[i];
                if (pair.Timestamp < minTimestamp) {
                    return false;
                }

                if (pair.Id == nodeId) {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns if the given node has been seen in a specific scope.
        /// </summary>
        public bool HasSeen(StringHash32 nodeId, ScriptNodeMemoryTarget memoryTarget) {
            switch (memoryTarget) {
                case ScriptNodeMemoryTarget.Untracked: {
                    for(int i = 0; i < RecentlyViewedNodeIds.Count; i++) {
                        if (RecentlyViewedNodeIds[i].Id == nodeId) {
                            return true;
                        }
                    }

                    return false;
                }

                default: {
                    int idx = (int) memoryTarget - 1;
                    Assert.True(idx >= 0 && idx < MaxMemoryTargets, "Invalid memory target value '{0}'", memoryTarget);
                    return VisitedNodesMap[idx].Contains(nodeId);
                }
            }
        }
    }
}