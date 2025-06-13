#ifndef OTOON_PBR_SURFACE_DATA_INCLUDED
#define OTOON_PBR_SURFACE_DATA_INCLUDED

struct OtoonPBRSurfaceData
{
    half stepViaRampTexture;
    half toonBlending;
    half diffuseStep;
    half halfToneUvMode;
    half specularFalloff;
    half specularSize;
    half halfToneEnabled;
    half4 halfToneColor;
    half halftoneNoiseClip;
    half brushLowerCut;
    half brushSize;
    float brushTilling;
    half halfToneDiffuseStep;
    half sizeFalloff;
    half halfToneIncludeReceivedShadow;
    half4 shadowColor;
    half specShadowStrength;
    float3 originPosWS;
    float3 posWS;
    half3 bitangent;
    half halftoneFadeDistance;
    half halftoneFadeToColor;
};

#endif