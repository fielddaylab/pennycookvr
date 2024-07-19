using System;
using FieldDay.Components;
using UnityEngine;

namespace Pennycook.Tablet {
    public class FixedCameraRefreshRate : BatchedComponent {
        public Camera[] Cameras;
        public float RefreshRate = 30;
        
        [NonSerialized] public bool Paused = false;
        [NonSerialized] public float TimeBeforeNextRefresh;
    }
}