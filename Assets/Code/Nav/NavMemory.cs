using BeauUtil;
using FieldDay;
using FieldDay.Scenes;

namespace Pennycook {
    static public partial class NavMemory {
        static private Unsafe.ArenaHandle s_Arena;

        [InvokePreBoot]
        static private void Initialize() {
            s_Arena = Game.Memory.CreateArena(3 * Unsafe.MiB, "Navigation", Unsafe.AllocatorFlags.ZeroOnAllocate);

            Game.Scenes.OnSceneUnload.Register((s) => {
                if (s.LoadType == SceneType.Persistent && s.Scene.name == "Exterior_Nav") {
                    s_Arena.Reset();
                }
            });

            GameLoop.OnShutdown.Register(() => {
                Game.Memory.DestroyArena(ref s_Arena);
            });
        }

        static public UnsafeBitSet CreateBitGrid(int width, int height) {
            return new UnsafeBitSet(s_Arena.AllocSpan<byte>(Unsafe.AlignUp32(width * height)).Cast<uint>());
        }

        static public UnsafeSpan<T> CreateGrid<T>(int width, int height) where T : unmanaged {
            return s_Arena.AllocSpan<T>(width * height);
        }
    }
}