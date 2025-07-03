#ifndef UNIVERSAL_OTOON_LIT_INPUT_INCLUDED
#define UNIVERSAL_OTOON_LIT_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "OToonPBRSurfaceData.hlsl"

// NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4 _BaseColor;
    half4 _SpecColor;
    half4 _EmissionColor;
    half _Cutoff;
    half _Smoothness;
    half _Metallic;
    half _BumpScale;
    half _OcclusionStrength;
    half _Surface;
    half _StepViaRampTexture;
    half _NoiseScale;
    half _NoiseStrength;
    half _ToonBlending;
    half _DiffuseStep;
    half _HalfToneUvMode;
    half _SpecClipMaskScale;
    half _SpecularClipStrength;
    half _SpecularFalloff;
    half _SpecularSize;
    half _HalfToneEnabled;
    half4 _HalfToneColor;
    half _HalftoneNoiseClip;
    half _BrushLowerCut;
    half _BrushSize;
    float _HalftoneTilling;
    half _HalfToneDiffuseStep;
    half _SizeFalloff;
    half _HalfToneIncludeReceivedShadow;
    half _HalftoneFadeDistance;
    half _HalftoneFadeToColor;
    half4 _ShadowColor;
    half _SpecShadowStrength;
    half _UseRampColor;
    half _FlattenGI;

    float4 _HalfTonePatternMap_ST;
    float4 _HalfToneNoiseMap_ST;
    float4 _HatchingNoiseMap_ST;
    float _OverrideShadowColor;
CBUFFER_END


TEXTURE2D(_OcclusionMap);       SAMPLER(sampler_OcclusionMap);
TEXTURE2D(_MetallicGlossMap);   SAMPLER(sampler_MetallicGlossMap);
TEXTURE2D(_SpecGlossMap);       SAMPLER(sampler_SpecGlossMap);

#ifdef _SPECULAR_SETUP
    #define SAMPLE_METALLICSPECULAR(uv) SAMPLE_TEXTURE2D(_SpecGlossMap, sampler_SpecGlossMap, uv)
#else
    #define SAMPLE_METALLICSPECULAR(uv) SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, uv)
#endif

half4 SampleMetallicSpecGloss(float2 uv, half albedoAlpha)
{
    half4 specGloss;

    #ifdef _METALLICSPECGLOSSMAP
        specGloss = SAMPLE_METALLICSPECULAR(uv);
        #ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            specGloss.a = albedoAlpha * _Smoothness;
        #else
            specGloss.a *= _Smoothness;
        #endif
    #else // _METALLICSPECGLOSSMAP
        #if _SPECULAR_SETUP
            specGloss.rgb = _SpecColor.rgb;
        #else
            specGloss.rgb = _Metallic.rrr;
        #endif

        #ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            specGloss.a = albedoAlpha * _Smoothness;
        #else
            specGloss.a = _Smoothness;
        #endif
    #endif

    return specGloss;
}

half SampleOcclusion(float2 uv)
{
    #ifdef _OCCLUSIONMAP
        // TODO: Controls things like these by exposing SHADER_QUALITY levels (low, medium, high)
        #if defined(SHADER_API_GLES)
            return SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, uv).g;
        #else
            half occ = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, uv).g;
            return LerpWhiteTo(occ, _OcclusionStrength);
        #endif
    #else
        return 1.0;
    #endif
}

inline void InitializeStandardLitSurfaceData(float2 uv, out SurfaceData outSurfaceData)
{
    half4 albedoAlpha = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
    outSurfaceData.alpha = Alpha(albedoAlpha.a, _BaseColor, _Cutoff);

    half4 specGloss = SampleMetallicSpecGloss(uv, albedoAlpha.a);
    outSurfaceData.albedo = albedoAlpha.rgb * _BaseColor.rgb;

    #if _SPECULAR_SETUP
        outSurfaceData.metallic = 1.0h;
        outSurfaceData.specular = specGloss.rgb;
    #else
        outSurfaceData.metallic = specGloss.r;
        outSurfaceData.specular = half3(0.0h, 0.0h, 0.0h);
    #endif

    outSurfaceData.smoothness = specGloss.a;
    outSurfaceData.normalTS = SampleNormal(uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);
    outSurfaceData.occlusion = SampleOcclusion(uv);
    outSurfaceData.emission = SampleEmission(uv, _EmissionColor.rgb, TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap));
    #if VERSION_GREATER_EQUAL(10, 0)
        outSurfaceData.clearCoatMask = 0.0h;
        outSurfaceData.clearCoatSmoothness = 0.0h;
    #endif
}

void InitializeOtoonPBRSurfaceData(out OtoonPBRSurfaceData otoonSurfaceData)
{
    otoonSurfaceData.stepViaRampTexture = _StepViaRampTexture;
    otoonSurfaceData.toonBlending = _ToonBlending;
    otoonSurfaceData.diffuseStep = _DiffuseStep;
    otoonSurfaceData.halfToneUvMode = _HalfToneUvMode;
    otoonSurfaceData.specularFalloff = _SpecularFalloff;
    otoonSurfaceData.specularSize = _SpecularSize;
    otoonSurfaceData.halfToneEnabled = _HalfToneEnabled;
    otoonSurfaceData.halfToneColor = _HalfToneColor;
    otoonSurfaceData.halftoneNoiseClip = _HalftoneNoiseClip;
    otoonSurfaceData.brushLowerCut = _BrushLowerCut;
    otoonSurfaceData.brushSize = _BrushSize;
    otoonSurfaceData.brushTilling = _HalftoneTilling;
    otoonSurfaceData.halfToneDiffuseStep = _HalfToneDiffuseStep;
    otoonSurfaceData.sizeFalloff = _SizeFalloff;
    otoonSurfaceData.halfToneIncludeReceivedShadow = _HalfToneIncludeReceivedShadow;
    otoonSurfaceData.shadowColor = _ShadowColor;
    otoonSurfaceData.specShadowStrength = _SpecShadowStrength;
    otoonSurfaceData.originPosWS = 0;
    otoonSurfaceData.posWS = 0;
    otoonSurfaceData.bitangent = 0;
    otoonSurfaceData.halftoneFadeDistance = _HalftoneFadeDistance;
    otoonSurfaceData.halftoneFadeToColor = _HalftoneFadeToColor;
}

#endif
