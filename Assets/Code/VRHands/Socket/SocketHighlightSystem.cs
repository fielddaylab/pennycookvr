using System.Collections.Generic;
using FieldDay.Systems;
using UnityEngine;

namespace FieldDay.Sockets {
    [SysUpdate(GameLoopPhase.LateFixedUpdate)]
    public class SocketHighlightSystem : ComponentSystemBehaviour<Socketable> {
        public override void ProcessWorkForComponent(Socketable component, float deltaTime) {
            if (!component.SocketEnabled || component.CurrentSocket || component.PotentialSockets.Count == 0) {
                UpdateHighlight(component, null);
                return;
            }

            UpdateHighlight(component, GetClosestSocket(component, component.PotentialSockets));
        }

        protected override void OnComponentRemoved(Socketable component) {
            UpdateHighlight(component, null);
        }

        static private void UpdateHighlight(Socketable socketable, ObjectSocket socket) {
            ref ObjectSocket current = ref socketable.HighlightedSocket;
            if (current == socket) {
                return;
            }

            if (current) {
                current.OnHighlightCountUpdated.Invoke(++current.HighlightCount);
            }

            current = socket;

            if (current) {
                current.OnHighlightCountUpdated.Invoke(--current.HighlightCount);
            }

            socketable.OnSocketHighlight.Invoke(socket);
        }

        static private ObjectSocket GetClosestSocket(Socketable socketable, HashSet<ObjectSocket> sockets) {
            Vector3 sourcePos = socketable.CachedTransform.position;
            ObjectSocket closestSocket = null;
            float closestDistSq = float.MaxValue;

            foreach(var socket in sockets) {
                if (socket.Locked || socket.Current != null || !socket.CanAdd(socket, socketable)) {
                    continue;
                }

                Vector3 socketPos = socket.Location.position;
                float distSq = Vector3.SqrMagnitude(socketPos - sourcePos);
                if (distSq < closestDistSq) {
                    closestSocket = socket;
                    closestDistSq = distSq;
                }
            }

            return closestSocket;
        }
    }
}