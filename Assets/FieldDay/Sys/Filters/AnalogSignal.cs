using System;
using UnityEngine;

namespace FieldDay.Filters {
    /// <summary>
    /// Analog signal tracker.
    /// </summary>
    [Serializable]
    public struct AnalogSignal {
        [Range(0, 1)] public float Analog;
        public bool Digital;

        /// <summary>
        /// Processes a frame of digital input.
        /// </summary>
        static public void Process(ref AnalogSignal signal, bool digitalState, float deltaTime, in SignalLatchWindow window, in SignalEnvelope ad) {
            if (digitalState) {
                if (signal.Analog < 1) {
                    signal.Analog = Math.Min(signal.Analog + deltaTime / ad.AttackDuration, 1);
                    if (signal.Analog >= window.OnThreshold) {
                        signal.Digital = true;
                    }
                }
            } else {
                if (signal.Analog > 0) {
                    signal.Analog = Math.Max(signal.Analog - deltaTime / ad.AttackDuration, 0);
                    if (signal.Analog <= window.OffThreshold) {
                        signal.Digital = false;
                    }
                }
            }
        }

        /// <summary>
        /// Forces the signal to a single digital state.
        /// </summary>
        static public void Reset(ref AnalogSignal signal, bool digitalState) {
            signal.Digital = digitalState;
            signal.Analog = digitalState ? 1 : 0;
        }
    }

    /// <summary>
    /// Signal latching window.
    /// </summary>
    [Serializable]
    public struct SignalLatchWindow {
        /// <summary>
        /// Value beyond which the signal's digital value is set to true.
        /// </summary>
        [Range(0, 1)] public float OnThreshold;

        /// <summary>
        /// Value beyond which the signal's digital value is set to false.
        /// </summary>
        [Range(0, 1)] public float OffThreshold;

        /// <summary>
        /// Default latching threshold.
        /// </summary>
        static public readonly SignalLatchWindow Default = new SignalLatchWindow() {
            OnThreshold = 0.5f,
            OffThreshold = 0.5f
        };

        /// <summary>
        /// Full latching threshold.
        /// </summary>
        static public readonly SignalLatchWindow Full = new SignalLatchWindow() {
            OnThreshold = 1f,
            OffThreshold = 0f
        };
    }

    /// <summary>
    /// Attack-decay timings.
    /// </summary>
    [Serializable]
    public struct SignalEnvelope {
        public float AttackDuration;
        public float DecayDuration;
    }
}