using System;
using UnityEngine;

namespace Pennycook {
    public struct PhysicsAutoSyncScope : IDisposable {
        private bool m_OldValue;

        public PhysicsAutoSyncScope(bool newValue) {
            m_OldValue = Physics.autoSyncTransforms;
            if (newValue != m_OldValue) {
                Physics.autoSyncTransforms = newValue;
            }
        }

        public void Dispose() {
            Physics.autoSyncTransforms = m_OldValue;
        }
    }
}