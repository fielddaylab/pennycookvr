using BeauUtil;

namespace Pennycook {
    static public class GameEvents {
        static public readonly StringHash32 ObjectHighlighted = "tablet:object-highlighted";
        static public readonly StringHash32 ObjectUnhighlighted = "tablet:object-unhighlighted";

        static public readonly StringHash32 PenguinReachedPathTarget = "penguin:reached-path-target";
        static public readonly StringHash32 PenguinPathingInterrupted = "penguin:pathing-interrupted";

        static public readonly StringHash32 WarpPointUpdated = "player:warp-point-updated";
    }

    static public class GameTriggers {
        static public readonly StringHash32 SceneReady = "SceneReady";
        static public readonly StringHash32 ScenePrepare = "ScenePrepare";

        static public readonly StringHash32 SceneUnload = "SceneUnload";

        static public readonly StringHash32 AtWarpPoint = "AtWarpPoint";
        static public readonly StringHash32 PlayerLookAtObject = "PlayerLookAt";

        static public readonly StringHash32 TabletLookAtObject = "TabletLookAt";

        static public readonly StringHash32 TabletPhotoTaken = "TabletPhotoTaken";
        static public readonly StringHash32 TabletNewBehaviorCaptured = "TabletBehaviorCaptured";
        static public readonly StringHash32 TabletNewBehaviorInstanceCaptured = "TabletBehaviorInstanceCaptured";
    }
}