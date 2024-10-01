using System;
using System.Collections.Generic;
using BeauUtil;
using FieldDay.Components;
using FieldDay.Scenes;
using UnityEngine;

namespace Pennycook {
    public class DistantPenguin : BatchedComponent, IScenePreload {
        public Transform MoveRoot;

        [NonSerialized] public Vector3 OriginalPos;
        [NonSerialized] public float RandomCycle;
        [NonSerialized] public float DistanceMult;
        [NonSerialized] public float SpeedMult;

        public IEnumerator<WorkSlicer.Result?> Preload() {
            OriginalPos = MoveRoot.position;
            return null;
        }
    }
}