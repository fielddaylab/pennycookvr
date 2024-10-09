using BeauUtil;

namespace Pennycook {
    static public class GameEvents {
        static public readonly StringHash32 ObjectHighlighted = "tablet:object-highlighted";
        static public readonly StringHash32 ObjectUnhighlighted = "tablet:object-unhighlighted";

        static public readonly StringHash32 PenguinReachedPathTarget = "penguin:reached-path-target";
        static public readonly StringHash32 PenguinPathingInterrupted = "penguin:pathing-interrupted";
    }

    static public class GameTriggers {
        static public readonly StringHash32 SceneReady = "SceneReady";
        static public readonly StringHash32 ScenePrepare = "ScenePrepare";

        static public readonly StringHash32 AtWarpPoint = "AtWarpPoint";
        static public readonly StringHash32 PlayerLookAtObject = "PlayerLookAt";
    }
}