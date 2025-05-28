using BeauUtil;
using FieldDay;
using FieldDay.Scripting;
using FieldDay.Sockets;
using FieldDay.VRHands;
using FieldDay.Debugging;
using BeauUtil.Debugger;

namespace Pennycook {
    static public class GrabTriggers {
        static public readonly StringHash32 ObjectGrabbed = "ObjectGrabbed";
        static public readonly StringHash32 ObjectDropped = "ObjectDropped";

        static public readonly StringHash32 ObjectSocketed = "ObjectSocketed";
        static public readonly StringHash32 ObjectUnsocketed = "ObjectUnsocketed";

        [InvokeOnBoot]
        static private void Initialize() {
            GrabUtility.OnObjectGrabbed.Register(OnGrabbed);
            GrabUtility.OnObjectReleased.Register(OnDropped);
            SocketUtility.OnObjectAddedToSocket.Register(OnSocketed);
            SocketUtility.OnObjectRemovedFromSocket.Register(OnUnsocketed);
        }

        static private void OnGrabbed(Grabbable grabbable, Grabber grabber, int snapIndex) {
            using (var table = TempVarTable.Alloc())
            {
                table.ActorInfo(ScriptUtility.Actor(grabbable));
                table.Set("hand", ChiralityToSymbol[(int)grabber.Chirality]);
                table.Set("bothHands", grabbable.CurrentGrabberCount >= 2);
                ScriptUtility.Trigger(ObjectGrabbed, table);

                VRGame.Events.Dispatch(GameEvents.ObjectGrabbed, EvtArgs.Create(new Pennycook.Data.GrababbleInfo(grabbable.gameObject.name, grabbable.Type)));
            }
        }

        static private void OnDropped(Grabbable grabbable, Grabber grabber, int snapIndex) {
            if (grabbable.CurrentGrabberCount == 0) {
                using (var table = TempVarTable.Alloc())
                {
                    table.ActorInfo(ScriptUtility.Actor(grabbable));
                    ScriptUtility.Trigger(ObjectDropped, table);
                    VRGame.Events.Dispatch(GameEvents.ObjectReleased, EvtArgs.Create(new Pennycook.Data.GrababbleInfo(grabbable.gameObject.name, grabbable.Type)));
                }
            }
        }

        static private void OnSocketed(Socketable socketable, ObjectSocket socket) {
            using(var table = TempVarTable.Alloc()) {
                
                table.ActorInfo(ScriptUtility.Actor(socketable));
                table.ActorInfo(ScriptUtility.Actor(socket), "socketId", "socketType");
                
                ScriptUtility.Trigger(ObjectSocketed, table);
            }
        }

        static private void OnUnsocketed(Socketable socketable, ObjectSocket socket) {
            using (var table = TempVarTable.Alloc()) {
                table.ActorInfo(ScriptUtility.Actor(socketable));
                table.ActorInfo(ScriptUtility.Actor(socket), "socketId", "socketType");
                ScriptUtility.Trigger(ObjectUnsocketed, table);
            }
        }

        static private readonly StringHash32[] ChiralityToSymbol = new StringHash32[] {
            "Left", "Right", ""
        };
    }
}