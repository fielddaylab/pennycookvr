#ifndef CUSTOM_LIGHTING_EXTEND_INCLUDED
#define CUSTOM_LIGHTING_EXTEND_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "OToonPBRSurfaceData.hlsl"

TEXTURE2D(_RampTex);                  SAMPLER(sampler_RampTex);
TEXTURE2D(_DiffuseWrapNoise);         SAMPLER(sampler_DiffuseWrapNoise);
TEXTURE2D(_SpecularClipMask);         SAMPLER(sampler_SpecularClipMask);
TEXTURE2D(_HairSpecNoiseMap);         SAMPLER(sampler_HairSpecNoiseMap);
TEXTURE2D(_HatchingNoiseMap);         SAMPLER(sampler_HatchingNoiseMap);
TEXTURE2D(_HalfToneNoiseMap);         SAMPLER(sampler_HalfToneNoiseMap);
TEXTURE2D(_HalfTonePatternMap);       SAMPLER(sampler_HalfTonePatternMap);
TEXTURE2D(_FaceShadowMap);            SAMPLER(sampler_FaceShadowMap);

half StepFeatherToon(half Term, half maxTerm, half step, half feather)
{
    return saturate((Term / maxTerm - step) / feather) * maxTerm;
}

half AdjustedNdotL(half3 normal, half3 lightDir, half offset, half noise)
{
    half NdotL = saturate(dot(normal, lightDir) - offset +noise);
    return NdotL;
}

half AdjustedNdotL(half3 normal, half3 lightDir, half offset)
{
    half NdotL = saturate(dot(normal, lightDir) - offset);
    return NdotL;
}

float Unity_Rectangle(float2 UV, float Width, float Height)
{
    float2 d = abs(UV * 2.0 - 1.0) / float2(Width, Height);
    return 1.0 - d.x;
}

float Unity_Ellipse(float2 UV, float Width, float Height)
{
    float d = length((UV * 2.0 - 1.0) / float2(Width, Height));
    return saturate((1 - d) / fwidth(d));
}

float Remap(float In, float2 InMinMax, float2 OutMinMax)
{
    return OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
}

float2 Rotate(float2 UV, float2 Center, float Rotation)
{
    //rotation matrix
    Rotation = Rotation * (3.1415926f / 180.0f);
    UV -= Center;
    float s = sin(Rotation);
    float c = cos(Rotation);

    //center rotation matrix
    float2x2 rMatrix = float2x2(c, -s, s, c);
    rMatrix *= 0.5;
    rMatrix += 0.5;
    rMatrix = rMatrix * 2 - 1;

    //multiply the UVs by the rotation matrix
    UV.xy = mul(UV.xy, rMatrix);
    UV += Center;

    return UV;
}

half StripeShapeValue(float2 uv, float frequency, float2 offset, float rotation, float size)
{
    uv = Rotate(uv, float2(0.5, 0.5), rotation);
    float2 UV = uv * float2(frequency, 1) + offset;
    return Unity_Rectangle(frac(UV), size, size);
}

float DotShapeValue(float2 uv, float2 scale, float2 offset, float sizeAdjust, float size)
{
    float2 dotUV = (uv * scale);
    float2 offsets = float2(0.5, 1);
    float alterX = dotUV.x + step(1, fmod(dotUV.y, 2)) * offsets.x;
    float alterY = dotUV.y + step(1, fmod(dotUV.x, 2)) * offsets.y;
    dotUV.x = alterX;
    dotUV.y = alterY;
    dotUV = frac(dotUV);
    float brushSize = size * sizeAdjust;
    return Unity_Ellipse(dotUV, brushSize, brushSize);
}

half DirectBRDFToonSpecular(BRDFData brdfData, half3 normalWS, half3 lightDirectionWS, half3 viewDirectionWS, OtoonPBRSurfaceData otoonSurface)
{
    float3 halfDir = SafeNormalize(float3(lightDirectionWS) + float3(viewDirectionWS));
    float NoH = saturate(dot(normalWS, halfDir));
    half LoH = saturate(dot(lightDirectionWS, halfDir));

    // GGX Distribution multiplied by combined approximation of Visibility and Fresnel
    // BRDFspec = (D * V * F) / 4.0
    // D = roughness^2 / ( NoH^2 * (roughness^2 - 1) + 1 )^2
    // V * F = 1.0 / ( LoH^2 * (roughness + 0.5) )
    // See "Optimizing PBR for Mobile" from Siggraph 2015 moving mobile graphics course
    // https://community.arm.com/events/1155

    // Final BRDFspec = roughness^2 / ( NoH^2 * (roughness^2 - 1) + 1 )^2 * (LoH^2 * (roughness + 0.5) * 4.0)
    // We further optimize a few light invariant terms
    // brdfData.normalizationTerm = (roughness + 0.5) * 4.0 rewritten as roughness * 4.0 + 2.0 to a fit a MAD.
    float d = NoH * NoH * brdfData.roughness2MinusOne + 1.00001f;

    half LoH2 = LoH * LoH;
    half specularTerm = brdfData.roughness2 / ((d * d) * max(0.1h, LoH2) * brdfData.normalizationTerm);
    // On platforms where half actually means something, the denominator has a risk of overflow
    // clamp below was added specifically to "fix" that, but dx compiler (we convert bytecode to metal/gles)
    // sees that specularTerm have only non-negative terms, so it skips max(0,..) in clamp (leaving only min(100,...))
    #if defined(SHADER_API_MOBILE) || defined(SHADER_API_SWITCH)
        specularTerm = specularTerm - HALF_MIN;
        specularTerm = clamp(specularTerm, 0.0, 100.0); // Prevent FP16 overflow on mobiles
    #endif

    half maxSpecularTerm = 1.0h / ((brdfData.roughness2MinusOne + 1.00001f) * max(0.1h, LoH2) * brdfData.normalizationTerm);
    specularTerm = StepFeatherToon(specularTerm, maxSpecularTerm, 1 - otoonSurface.specularSize, otoonSurface.specularFalloff);
    return specularTerm;
}

half3 LightingSpecularToon(half3 lightColor, half3 lightDir, half3 normal, half3 viewDir, half4 specular, half size, half smoothness)
{
    half reverseSize = 1 - size;
    float3 halfVec = SafeNormalize(float3(lightDir) + float3(viewDir));
    half NdotH = saturate(dot(normal, halfVec));
    half spec = pow(NdotH, reverseSize);
    spec = StepFeatherToon(spec, 1, reverseSize, smoothness);
    half3 specularReflection = specular.rgb * spec;
    return lightColor * specularReflection;
}

float SampleHalfTone(float2 uv, float2 objectUV, float3 normalWS, float3 lightDirection, float lightAttenuation, OtoonPBRSurfaceData otoonSurface)
{
    float halfTone = 0;
    float cutNoise = SAMPLE_TEXTURE2D(_HalfToneNoiseMap, sampler_HalfToneNoiseMap, objectUV * _HalfToneNoiseMap_ST.xy + _HatchingNoiseMap_ST.zw).r * otoonSurface.halftoneNoiseClip;
    float haltoneNdotL = saturate(dot(normalWS, lightDirection) - otoonSurface.halfToneDiffuseStep);
    float shadowedNdotL = lerp(haltoneNdotL, haltoneNdotL * lightAttenuation, otoonSurface.halfToneIncludeReceivedShadow);
    float shaded = 1 - shadowedNdotL;
    #if defined(_HALFTONESHAPE_CUSTOM)
        float shapeIn1 = SAMPLE_TEXTURE2D(_HalfTonePatternMap, sampler_HalfTonePatternMap, uv * otoonSurface.brushTilling).a;
        float sizeFalloff = lerp(1, (1 - shaded), otoonSurface.sizeFalloff);
        shapeIn1 = step((1 - otoonSurface.brushSize) * sizeFalloff, shapeIn1);
        halfTone = shapeIn1;
        float patternValue = Remap(shapeIn1, float2(0, 1), float2(0, 1 - otoonSurface.brushLowerCut));
        halfTone = step(patternValue, shadowedNdotL + cutNoise);
    #endif // _HALFTONESHAPE_CUSTOM

    #if defined(_HALFTONESHAPE_DOT)
        float dotSize = smoothstep(shadowedNdotL - otoonSurface.sizeFalloff, shadowedNdotL + otoonSurface.sizeFalloff, 0.5) * otoonSurface.brushSize;
        float shapeIn1 = DotShapeValue(uv, otoonSurface.brushTilling, 0, 1, dotSize);
        halfTone = 1 - shapeIn1;
    #endif // _HALFTONESHAPE_DOT

    #if defined(_HALFTONESHAPE_STRIPE)
        float shapeIn1 = StripeShapeValue(uv, otoonSurface.brushTilling, 0, -45, lerp(1, shaded, otoonSurface.sizeFalloff) * otoonSurface.brushSize);
        float dotValue1 = Remap(shapeIn1, float2(0, 1), float2(0, 1 - otoonSurface.brushLowerCut * 2));
        halfTone = step(dotValue1 * step(shadowedNdotL, 0.5), cutNoise);
    #endif // _HALFTONESHAPE_STRIPE

    #if defined(_HALFTONESHAPE_CROSS)
        float shapeIn1 = StripeShapeValue(uv, otoonSurface.brushTilling, 0, -45, lerp(otoonSurface.brushSize, shaded * otoonSurface.brushSize, otoonSurface.sizeFalloff));
        float dotValue1 = Remap(shapeIn1, float2(0, 1), float2(0, 1 - otoonSurface.brushLowerCut));
        float shapeIn2 = StripeShapeValue(uv, otoonSurface.brushTilling, 0, 45, lerp(otoonSurface.brushSize, shaded * otoonSurface.brushSize, otoonSurface.sizeFalloff));
        float dotValue2 = Remap(shapeIn2, float2(0, 1), float2(0, 1 - otoonSurface.brushLowerCut));
        halfTone = step(dotValue1, haltoneNdotL + cutNoise);
        halfTone *= step(dotValue2, haltoneNdotL + cutNoise);
    #endif // _HALFTONESHAPE_CROSS

    return halfTone;
}

/*

The face shadow map logic is taken from github repo by user Ash Yukiha.
link : https://github.com/ashyukiha/GenshinCharacterShaderZhihuVer
MIT License

Copyright (c) 2021 Ash Yukiha

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

half3 LightingPhysicallyBased_Extend(BRDFData brdfData, BRDFData brdfDataClearCoat,
Light light,
half3 normalWS, half3 viewDirectionWS,
half clearCoatMask, bool specularHighlightsOff, float3 positionWS, float2 uv, float2 screenUV, OtoonPBRSurfaceData otoonSurface)
{
    half NdotL = saturate(dot(normalWS, light.direction));
    half lightAttenuation = light.distanceAttenuation * light.shadowAttenuation;

    half3 baseColor = brdfData.diffuse;
    half3 spec = 0;
    half3 outColor = baseColor * NdotL; // default PBR surface
    half shadowPower = otoonSurface.shadowColor.a;
    half3 adjustShadowColor = 0;

    half diffuseWrapNoise = 0.h;

    #ifdef _TOON_SHADING_ON
        adjustShadowColor = otoonSurface.shadowColor.rgb;
        NdotL = saturate(dot(normalWS, light.direction) - otoonSurface.diffuseStep);
        
        half rampNdotL = AdjustedNdotL(normalWS, light.direction, otoonSurface.diffuseStep, diffuseWrapNoise);
        half StepNdotL = lerp(step(0.5, NdotL + diffuseWrapNoise), SAMPLE_TEXTURE2D(_RampTex, sampler_RampTex, float2(rampNdotL, 0.5)).r, otoonSurface.stepViaRampTexture);
        half3 toneBase = _BaseColor.rgb * SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)).rgb;
        half3 toonColor = lerp(lerp(adjustShadowColor, toneBase, 1 - shadowPower), toneBase, lerp(NdotL, StepNdotL, otoonSurface.toonBlending));
        outColor = lerp(baseColor * NdotL, toonColor, otoonSurface.toonBlending);
        
    #endif // _TOON_SHADING

    #ifndef _SPECULARHIGHLIGHTS_OFF
        [branch] if (!specularHighlightsOff)
        {
            spec = brdfData.specular * DirectBRDFToonSpecular(brdfData, normalWS, light.direction, viewDirectionWS, otoonSurface);
        }
    #endif

    // CUSTOM SHADOW COLOR
    outColor = lerp(lerp(outColor, adjustShadowColor, shadowPower), outColor, light.shadowAttenuation);
    // CUSTOM SHADOW  COLOR


    //HalfTone Effect
    if (otoonSurface.halfToneEnabled > 0)
    {
        half halfToneImpact = 1 - SampleHalfTone(lerp(uv, screenUV, otoonSurface.halfToneUvMode), uv, normalWS, light.direction, lightAttenuation, otoonSurface);
        half halftoneAlphaT = Remap(max(otoonSurface.halftoneFadeDistance - distance(_WorldSpaceCameraPos, positionWS), 0), float2(0, otoonSurface.halftoneFadeDistance), float2(0, 1));
        half haltoneNdotL = 1 - saturate(dot(normalWS, light.direction) - otoonSurface.halfToneDiffuseStep);
        half fadeShaded = lerp(0, haltoneNdotL, otoonSurface.halftoneFadeToColor);
        outColor = lerp(outColor, otoonSurface.halfToneColor.rgb, lerp(fadeShaded, halfToneImpact, halftoneAlphaT) * otoonSurface.halfToneColor.a);
    }

    //Apply all Speculars
    outColor += lerp(spec * light.shadowAttenuation, spec, otoonSurface.specShadowStrength);
    //Apply all Speculars

    outColor.rgb *= light.color * light.distanceAttenuation;
    return outColor;
}

half3 LightingToon_Extend(Light light, half3 diffuse, half4 specColor,
half3 normalWS, half3 viewDirectionWS, float3 positionWS, float2 uv, float2 screenUV, OtoonPBRSurfaceData otoonSurface)
{
    #ifdef _SPECULARHIGHLIGHTS_OFF
        bool specularHighlightsOff = true;
    #else
        bool specularHighlightsOff = false;
    #endif
    half NdotL = saturate(dot(normalWS, light.direction));
    half lightAttenuation = light.distanceAttenuation * light.shadowAttenuation;

    half3 spec = 0;
    half3 outColor = diffuse;

    half shadowPower = otoonSurface.shadowColor.a;
    half3 adjustShadowColor = 0;

    half diffuseWrapNoise = 0.h;

    NdotL = saturate(dot(normalWS, light.direction) - _DiffuseStep);

    adjustShadowColor = otoonSurface.shadowColor.rgb;
    half rampNdotL = AdjustedNdotL(normalWS, light.direction, otoonSurface.diffuseStep, diffuseWrapNoise);
    if (_UseRampColor == 0)
    {
        half StepNdotL = lerp(step(0.5, NdotL + diffuseWrapNoise), SAMPLE_TEXTURE2D(_RampTex, sampler_RampTex, float2(rampNdotL, 0.5)).r, otoonSurface.stepViaRampTexture);
        half3 toonColor = lerp(lerp(outColor, adjustShadowColor, shadowPower), outColor, StepNdotL);
        outColor = toonColor;
    }
    else
    {
        adjustShadowColor = lerp(adjustShadowColor, diffuse * SAMPLE_TEXTURE2D(_RampTex, sampler_RampTex, half2(0, 0.5)).rgb, _OverrideShadowColor);
        outColor = diffuse * SAMPLE_TEXTURE2D(_RampTex, sampler_RampTex, float2(rampNdotL, 0.5)).rgb;
    }

    #ifndef _SPECULARHIGHLIGHTS_OFF
        [branch] if (!specularHighlightsOff)
        {
            spec = LightingSpecularToon(light.color, light.direction, normalWS, viewDirectionWS, specColor, otoonSurface.specularSize, otoonSurface.specularFalloff);
        }
    #endif // _SPECULARHIGHLIGHTS_OFF

    // CUSTOM SHADOW COLOR
    outColor = lerp(lerp(outColor, adjustShadowColor, shadowPower), outColor, light.shadowAttenuation);
    // CUSTOM SHADOW  COLOR
    
    //HalfTone Effect
    if (otoonSurface.halfToneEnabled > 0)
    {
        half halfToneImpact = 1 - SampleHalfTone(lerp(uv, screenUV, otoonSurface.halfToneUvMode), uv, normalWS, light.direction, lightAttenuation, otoonSurface);
        half halftoneAlphaT = Remap(max(otoonSurface.halftoneFadeDistance - distance(_WorldSpaceCameraPos, positionWS), 0), float2(0, otoonSurface.halftoneFadeDistance), float2(0, 1));
        half haltoneNdotL = 1 - saturate(dot(normalWS, light.direction) - otoonSurface.halfToneDiffuseStep);
        half fadeShaded = lerp(0, haltoneNdotL, otoonSurface.halftoneFadeToColor);
        outColor = lerp(outColor, otoonSurface.halfToneColor.rgb, lerp(fadeShaded, halfToneImpact, halftoneAlphaT) * otoonSurface.halfToneColor.a);
    }

    //Apply all Speculars
    outColor += lerp(spec * light.shadowAttenuation, spec, otoonSurface.specShadowStrength);
    //Apply all Speculars

    outColor.rgb *= light.color * light.distanceAttenuation;
    return outColor;
}

half4 UniversalFragmentPBR_Extend(InputData inputData, SurfaceData surfaceData, float2 uv, float2 screenUV, OtoonPBRSurfaceData otoonSurface)
{
    #ifdef _SPECULARHIGHLIGHTS_OFF
        bool specularHighlightsOff = true;
    #else
        bool specularHighlightsOff = false;
    #endif

    BRDFData brdfData;

    InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.alpha, brdfData);

    BRDFData brdfDataClearCoat = (BRDFData)0;

    // To ensure backward compatibility we have to avoid using shadowMask input, as it is not present in older shaders
    #if defined(SHADOWS_SHADOWMASK) && defined(LIGHTMAP_ON)
        half4 shadowMask = inputData.shadowMask;
    #elif !defined(LIGHTMAP_ON)
        half4 shadowMask = unity_ProbesOcclusion;
    #else
        half4 shadowMask = half4(1, 1, 1, 1);
    #endif

    Light mainLight = GetMainLight(inputData.shadowCoord, inputData.positionWS, shadowMask);

    #if VERSION_GREATER_EQUAL(12, 0)
        #if defined(_SCREEN_SPACE_OCCLUSION)
            AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(inputData.normalizedScreenSpaceUV);
            mainLight.color *= aoFactor.directAmbientOcclusion;
            inputData.bakedGI *= aoFactor.indirectAmbientOcclusion;
        #endif
    #endif

    
    MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI, half4(0, 0, 0, 0));
    
    half3 color = GlobalIllumination(brdfData, inputData.bakedGI, surfaceData.occlusion, inputData.normalWS, inputData.viewDirectionWS);
    
        #ifdef _LIGHT_LAYERS
    uint meshRenderingLayers = GetMeshRenderingLayer();

    if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
        #endif
    {
        color += LightingPhysicallyBased_Extend(brdfData, brdfDataClearCoat,
        mainLight,
        inputData.normalWS, inputData.viewDirectionWS,
        0, specularHighlightsOff, inputData.positionWS, uv, screenUV, otoonSurface);
    }

#if defined(_ADDITIONAL_LIGHTS)
    uint pixelLightCount = GetAdditionalLightsCount();

    #if USE_FORWARD_PLUS
    
    for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
    {
        FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK

        Light light = GetAdditionalLight(lightIndex, inputData.positionWS, shadowMask);
        #ifdef _LIGHT_LAYERS
        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
            #endif
        {
            color += LightingPhysicallyBased_Extend(brdfData, brdfDataClearCoat,
             light,
             inputData.normalWS, inputData.viewDirectionWS,
             0, specularHighlightsOff, inputData.positionWS, uv, screenUV, otoonSurface);
        }
    }
    #endif

    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light light = GetAdditionalLight(lightIndex, inputData.positionWS, shadowMask);
    #ifdef _LIGHT_LAYERS
    if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
        #endif
    {
        color += LightingPhysicallyBased_Extend(brdfData, brdfDataClearCoat,
           light,
           inputData.normalWS, inputData.viewDirectionWS,
           0, specularHighlightsOff, inputData.positionWS, uv, screenUV, otoonSurface);
    }
    LIGHT_LOOP_END
#endif

    #ifdef _ADDITIONAL_LIGHTS_VERTEX
        color += inputData.vertexLighting * brdfData.diffuse;
    #endif

    color += surfaceData.emission;

    return half4(color, surfaceData.alpha);
}


half4 UniversalFragmentToon_Extend(InputData inputData, half3 diffuse, half4 specularGloss, half smoothness, half3 emission, half alpha, float2 uv, float2 screenUV, OtoonPBRSurfaceData otoonSurface)
{
    #if defined(SHADOWS_SHADOWMASK) && defined(LIGHTMAP_ON)
        half4 shadowMask = inputData.shadowMask;
    #elif !defined(LIGHTMAP_ON)
        half4 shadowMask = unity_ProbesOcclusion;
    #else
        half4 shadowMask = half4(1, 1, 1, 1);
    #endif

    Light mainLight = GetMainLight(inputData.shadowCoord, inputData.positionWS, shadowMask);

    #if VERSION_GREATER_EQUAL(12, 0)
        #if defined(_SCREEN_SPACE_OCCLUSION)
            AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(inputData.normalizedScreenSpaceUV);
            mainLight.color *= aoFactor.directAmbientOcclusion;
            inputData.bakedGI *= aoFactor.indirectAmbientOcclusion;
        #endif
    #endif

    MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI, half4(0, 0, 0, 0));

    BRDFData brdfData;
    half3 albedo = _BaseColor.rgb * SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)).rgb;

    InitializeBRDFData(albedo, 1.0 - 1.0 / kDieletricSpec.a, 0, 0, alpha, brdfData);
    half3 color = GlobalIllumination(brdfData, inputData.bakedGI, 1.0, inputData.normalWS, inputData.viewDirectionWS);
    
    #ifdef _LIGHT_LAYERS
    uint meshRenderingLayers = GetMeshRenderingLayer();
    if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
    #endif
    {
        color += LightingToon_Extend(mainLight, albedo, specularGloss, inputData.normalWS, inputData.viewDirectionWS, inputData.positionWS, uv, screenUV, otoonSurface);
    }

    #if defined(_ADDITIONAL_LIGHTS)
        uint pixelLightCount = GetAdditionalLightsCount();

        #if USE_FORWARD_PLUS
        
        for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
        {
            FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK

            Light light = GetAdditionalLight(lightIndex, inputData.positionWS, shadowMask);
            #ifdef _LIGHT_LAYERS
            if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
                #endif
            {
                 color += LightingToon_Extend(light, albedo, specularGloss, inputData.normalWS, inputData.viewDirectionWS, inputData.positionWS, uv, screenUV, otoonSurface);
            }
        }
        #endif

    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light light = GetAdditionalLight(lightIndex, inputData.positionWS, shadowMask);
        #ifdef _LIGHT_LAYERS
        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
            #endif
        {
            color += LightingToon_Extend(light, albedo, specularGloss, inputData.normalWS, inputData.viewDirectionWS, inputData.positionWS, uv, screenUV, otoonSurface);
        }
    LIGHT_LOOP_END
    #endif

    #ifdef _ADDITIONAL_LIGHTS_VERTEX
        color += inputData.vertexLighting;
    #endif

    color += emission;
    
    return half4(color, alpha);
}

#endif