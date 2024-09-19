using System;
using System.Collections;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using UnityEngine;

namespace Pennycook {
    public sealed class PenguinNavTest : MonoBehaviour {
        public Transform TargetPos;
        public float MoveSpeed = 3;

        [NonSerialized] public NavPath Path;
        [NonSerialized] public UniqueId16 PathRequestId;
        public Routine MoveRoutine;

        private void Awake() {
            Game.Scenes.QueueOnLoad(this, OnSceneLoaded);
            MoveSpeed *= RNG.Instance.NextFloat(0.7f, 1.2f);
        }

        private void OnDestroy() {
            PenguinNav.FreeNavPath(ref Path);
            PenguinNav.CancelPath(ref PathRequestId);
            MoveRoutine.Stop();
        }

        private void OnSceneLoaded() {
            SubmitRequest();
        }

        private void SubmitRequest() {
            PenguinNav.FreeNavPath(ref Path);
            PenguinNav.CancelPath(ref PathRequestId);
            MoveRoutine.Stop();

            PathRequestId = PenguinNav.RequestPath(transform.position, TargetPos.position + RNG.Instance.NextVector3(0.2f, 1.5f), OnPathResolved, this);
        }

        private IEnumerator MoveAlongPath() {
            while(Path.Positions.TryPopFront(out Vector3 pos)) {
                yield return transform.MoveToWithSpeed(pos, MoveSpeed);
            }
            PenguinNav.FreeNavPath(ref Path);
        }

        static private void OnPathResolved(NavPath path, object context) {
            PenguinNavTest comp = (PenguinNavTest) context;
            comp.Path = path;

            if (path != null) {
                Log.Msg("found path!");
                comp.PathRequestId = default;

                comp.Path = path;
                comp.MoveRoutine.Replace(comp, comp.MoveAlongPath());
            } else {
                Log.Warn("no path found...");
            }
        }
    }
}