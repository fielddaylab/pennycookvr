using System;


static public class LayerMasks {
    
    // Layer 0: Default
    public const int Default_Index = 0;
    public const int Default_Mask = 1;
    // Layer 1: TransparentFX
    public const int TransparentFX_Index = 1;
    public const int TransparentFX_Mask = 2;
    // Layer 2: Ignore Raycast
    public const int IgnoreRaycast_Index = 2;
    public const int IgnoreRaycast_Mask = 4;
    // Layer 3: Solid
    public const int Solid_Index = 3;
    public const int Solid_Mask = 8;
    // Layer 4: Water
    public const int Water_Index = 4;
    public const int Water_Mask = 16;
    // Layer 5: UI
    public const int UI_Index = 5;
    public const int UI_Mask = 32;
    // Layer 6: PlayerHand
    public const int PlayerHand_Index = 6;
    public const int PlayerHand_Mask = 64;
    // Layer 7: Grabbable
    public const int Grabbable_Index = 7;
    public const int Grabbable_Mask = 128;
    // Layer 8: Highlightable
    public const int Highlightable_Index = 8;
    public const int Highlightable_Mask = 256;
    // Layer 9: Warpable
    public const int Warpable_Index = 9;
    public const int Warpable_Mask = 512;
    // Layer 10: SeeThroughSolid
    public const int SeeThroughSolid_Index = 10;
    public const int SeeThroughSolid_Mask = 1024;
    // Layer 11: ExcludeDLight
    public const int ExcludeDLight_Index = 11;
    public const int ExcludeDLight_Mask = 2048;
    // Layer 12: Terrain
    public const int Terrain_Index = 12;
    public const int Terrain_Mask = 4096;
    // Layer 13: PlayerFinger
    public const int PlayerFinger_Index = 13;
    public const int PlayerFinger_Mask = 8192;
    // Layer 14: Button
    public const int Button_Index = 14;
    public const int Button_Mask = 16384;
    // Layer 15: LookTag
    public const int LookTag_Index = 15;
    public const int LookTag_Mask = 32768;
    // Layer 31: OffscreenRendering
    public const int OffscreenRendering_Index = 31;
    public const int OffscreenRendering_Mask = -2147483648;
}
static public class SortingLayers {
    
    // Layer Default
    public const int Default = 0;
    // Layer ForegroundCanvas
    public const int ForegroundCanvas = 1320863815;
}
static public class UnityTags {
    
    // Tag Untagged
    public const string Untagged = "Untagged";
    // Tag Respawn
    public const string Respawn = "Respawn";
    // Tag Finish
    public const string Finish = "Finish";
    // Tag EditorOnly
    public const string EditorOnly = "EditorOnly";
    // Tag MainCamera
    public const string MainCamera = "MainCamera";
    // Tag Player
    public const string Player = "Player";
    // Tag GameController
    public const string GameController = "GameController";
}