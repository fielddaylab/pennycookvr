using UnityEngine;
using BeauUtil;
using BeauUtil.Variants;
using System;
using FieldDay;

namespace Pennycook {

    
    public static class EnumLookup {
        public static readonly string[] Speaker = new string[] {
            "PLAYER", "MARGO", "PENNY"
        };
        public static readonly string[] DialogType = new string[] {
            "STORY", "COMMENTARY"
        };
        public static readonly string[] HighlightType = new string[] {
            "OBJECT", "DESTINATION"
        };
        public static readonly string[] Location = new string[] {
            "INSIDE", "OUTSIDE"
        };
        public static readonly string[] MargoModes = new string[] {
            "SCAN", "WARP", "PHOTO", "COUNT"
        };
        public static readonly string[] TaskTypes = new string[] {
            "SCAN", "COUNT", "CAPTURE", "TAG", "RECOVER"
        };

        public static readonly string[] TagTypes = new string[] {
            "LEG", "ARM", "BACK"
        };

        public static readonly string[] WarpPoints = new string[] {
            "WarpDesk", "WarpBluff", "WarpBluffBack", "WarpRookery", "WarpRookeryBack", 
            "WarpRookeryFar", "WarpRookerySide", "WarpRookeryMiddle", "WarpRookeryCorral"
        };

        public static readonly string[] BehaviorType = new string[] {
            "MATING_DANCE", "REGURGITATION"
        };
        public static readonly string[] DockLocation = new string[] {
            "TENT", "CASE"
        };

        public static readonly string[] GrabbableObject = new string[] {
            "PROP", "MARGO", "LEG_BAND", "PENGUIN", "ARM_BAND", "BACK_TRACKER"
        };
        
        public static readonly string[] TagLocations = new string[] {
            "CASE", "PENGUIN"
        };
        public static readonly string[] RelativeDirections = new string[] {
            "FORWARD", "BACKWARD", "LEFT", "RIGHT"
        };

        public static readonly string[] RotationDirections = new string[] {
            "CW", "CCW"
        };

        public static readonly string[] ScannableObjects = new string[] {
            "PENGUIN", "CHICK", "EGG", "NEST", "GATE"
        };

        public static string Get<T>(T e) where T : unmanaged, Enum {
            return GenericLookup<T>.Names[Enums.ToInt(e)];
        }

        static private class GenericLookup<T> where T : unmanaged, Enum {
            static internal readonly string[] Names;

            static GenericLookup() {
                string[] strings = Enum.GetNames(typeof(T));
                for(int i = 0; i < strings.Length; i++) {
                    strings[i] = ReflectionCache.AnalyticsNamePascal(strings[i]);
                }
                Names = strings;
            }
        }
     }

    static public class GameEvents {
        static public readonly StringHash32 ObjectHighlighted = "tablet:object-highlighted";
        static public readonly StringHash32 ObjectUnhighlighted = "tablet:object-unhighlighted";

        static public readonly StringHash32 PenguinReachedPathTarget = "penguin:reached-path-target";
        static public readonly StringHash32 PenguinPathingInterrupted = "penguin:pathing-interrupted";

        static public readonly StringHash32 WarpPointUpdated = "player:warp-point-updated";

        static public readonly StringHash32 DayCompleted = "player:day-completed";

        static public readonly StringHash32 PlayerGrab = "player:grab";
        static public readonly StringHash32 PlayerRelease = "player:release";

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