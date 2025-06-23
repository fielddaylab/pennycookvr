struct LightStruct
{
    half3  color;
    half3  direction;
    half   distanceAttenuation;
    half   shadowAttenuation;
    uint   layerMask;
};

#ifndef SHADERGRAPH_PREVIEW
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
#if (SHADERPASS != SHADERPASS_FORWARD)
#undef REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR
#endif
#endif

half StepFeatherToon(half Term, half maxTerm, half step, half feather)
{
    return saturate((Term / maxTerm - step) / feather) * maxTerm;
}

void MainLight_half(half3 positionWS, out half3 Direction, out half3 Color, out half DistanceAtten, out half ShadowAtten)
{
    #if defined(SHADERGRAPH_PREVIEW)
        Direction = half3(0.5, 0.5, 0);
        Color = 1;
        DistanceAtten = 1;
        ShadowAtten = 1;
    #else
        #if defined(_RECEIVE_SHADOWS_OFF)
            Light mainLight = GetMainLight();
            ShadowAtten = 1.0h;
            Direction = mainLight.direction;
            Color = mainLight.color;
            DistanceAtten = mainLight.distanceAttenuation;
        #else
            float4 shadowCoord = TransformWorldToShadowCoord(positionWS);
            
            #if VERSION_GREATER_EQUAL(10, 1)
                ShadowAtten = MainLightShadow(shadowCoord, positionWS, half4(1, 1, 1, 1), _MainLightOcclusionProbes);
            #else
                ShadowAtten = MainLightRealtimeShadow(shadowCoord);
            #endif

            Light mainLight = GetMainLight(shadowCoord);
            Direction = mainLight.direction;
            Color = mainLight.color;
            DistanceAtten = mainLight.distanceAttenuation;
        #endif
    #endif
}

void MainLight_float(half3 positionWS, out half3 Direction, out half3 Color, out half DistanceAtten, out half ShadowAtten)
{
    #if defined(SHADERGRAPH_PREVIEW)
        Direction = half3(0.5, 0.5, 0);
        Color = 1;
        DistanceAtten = 1;
        ShadowAtten = 1;
    #else
        #if defined(_RECEIVE_SHADOWS_OFF)
            Light mainLight = GetMainLight();
            ShadowAtten = 1.0h;
            Direction = mainLight.direction;
            Color = mainLight.color;
            DistanceAtten = mainLight.distanceAttenuation;
        #else
            float4 shadowCoord = TransformWorldToShadowCoord(positionWS);
            
            #if VERSION_GREATER_EQUAL(10, 1)
                ShadowAtten = MainLightShadow(shadowCoord, positionWS, half4(1, 1, 1, 1), _MainLightOcclusionProbes);
            #else
                ShadowAtten = MainLightRealtimeShadow(shadowCoord);
            #endif

            Light mainLight = GetMainLight(shadowCoord);
            Direction = mainLight.direction;
            Color = mainLight.color;
            DistanceAtten = mainLight.distanceAttenuation;
        #endif
    #endif
}

void Shadowmask_half (float2 lightmapUV, out half4 Shadowmask){
    #ifdef SHADERGRAPH_PREVIEW
        Shadowmask = half4(1,1,1,1);
    #else
    OUTPUT_LIGHTMAP_UV(lightmapUV, unity_LightmapST, lightmapUV);
        Shadowmask = SAMPLE_SHADOWMASK(lightmapUV);
    #endif
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

void ProceduralCrossHatching_half(Texture2D hatchMap, SamplerState state, half2 scale, float2 uv, half ndotl, half hatchingRotation, half hatchingDrawStrength, half hatchingSmoothness, half hatchingDensity, out half Out)
{
    half hatching = 1.0;
    half p = 1.0;
    half realNdotL = 1 - ndotl;
    float2 uv1 = Rotate(uv.xy, float2(0.5, 0.5), hatchingRotation);
    float2 uv2 = Rotate(uv1.xy, float2(0.5, 0.5), 90);
    float2 currentUV = uv1;
    float currentScale = 1.0;
    half sizeUpper = ndotl * hatchingDrawStrength * 0.1;

    const uint count = 15;
    for (uint i = 0; i < count; i++)
    {
        currentUV = lerp(uv1, uv2, i & 0x1);
        float g = SAMPLE_TEXTURE2D_LOD(hatchMap, state, scale * currentUV * currentScale, 0).r;
        g = 1.0 - smoothstep(0.5 - hatchingSmoothness, 0.5 + hatchingSmoothness + 0.1, sizeUpper - g);
        hatching = min(g, hatching);
        currentScale *= 1.2;
        
        if ((half)i > (smoothstep(0.5, 0.5 + (2 - hatchingDensity), ndotl) * hatchingDrawStrength))
        {
            break;
        }
    }
    Out = hatching;
}

void ProceduralCrossHatching_float(Texture2D hatchMap, SamplerState state, half2 scale, float2 uv, half ndotl, half hatchingRotation, half hatchingDrawStrength, half hatchingSmoothness, half hatchingDensity, out half Out)
{
    half hatching = 1.0;
    half p = 1.0;
    half realNdotL = 1 - ndotl;
    float2 uv1 = Rotate(uv.xy, float2(0.5, 0.5), hatchingRotation);
    float2 uv2 = Rotate(uv1.xy, float2(0.5, 0.5), 90);
    float2 currentUV = uv1;
    float currentScale = 1.0;
    half sizeUpper = ndotl * hatchingDrawStrength * 0.1;

    const uint count = 15;
    for (uint i = 0; i < count; i++)
    {
        currentUV = lerp(uv1, uv2, i & 0x1);
        float g = SAMPLE_TEXTURE2D_LOD(hatchMap, state, scale * currentUV * currentScale, 0).r;
        g = 1.0 - smoothstep(0.5 - hatchingSmoothness, 0.5 + hatchingSmoothness + 0.1, sizeUpper - g);
        hatching = min(g, hatching);
        currentScale *= 1.2;
        
        if ((half)i > (smoothstep(0.5, 0.5 + (2 - hatchingDensity), ndotl) * hatchingDrawStrength))
        {
            break;
        }
    }
    Out = hatching;
}


void ToonShading(LightStruct light, half4 shadowColor, half3 diffuse, half3 positionWS, half3 normalWS, half3 viewDirectionWS, float2 UV, float2 screenUV, half diffuseStep, half rampLighting, Texture2D rampTex, SamplerState state, out half3 Out)
{
    // Out = diffuse;
    half3 outColor = diffuse;
    half4 customShadowColor = shadowColor;
    half NdotL = saturate(dot(normalWS, light.direction) - diffuseStep);
    half StepNdotL = lerp(step(0.5, NdotL), SAMPLE_TEXTURE2D(rampTex, state, float2(NdotL, 0.5)).r, rampLighting);

    outColor = lerp(lerp(outColor, customShadowColor.rgb, customShadowColor.a), outColor, StepNdotL);
    
    // CUSTOM SHADOW COLOR
    outColor = lerp(lerp(outColor, customShadowColor.rgb, customShadowColor.a), outColor, light.shadowAttenuation);
    // CUSTOM SHADOW  COLOR

    outColor *= light.color * light.distanceAttenuation;
    Out = outColor;
}

void RampLighting_half(half4 shadowColor, half3 GI, half3 diffuse, half3 positionWS, half3 normalWS,
    half3 viewDirectionWS, float2 UV, float2 screenUV, half diffuseStep, half rampLighting,
    Texture2D rampTex, SamplerState state, half4 shadowMask, out half3 Out)
{
    LightStruct mainLight;
    MainLight_half(positionWS, mainLight.direction, mainLight.color, mainLight.distanceAttenuation, mainLight.shadowAttenuation);
    mainLight.shadowAttenuation = saturate(mainLight.shadowAttenuation);
    
    Out = GI;
    half3 mainLightShaded = 0;
    ToonShading(mainLight, shadowColor, diffuse, positionWS, normalWS, viewDirectionWS, UV, screenUV, diffuseStep,
        rampLighting, rampTex, state, mainLightShaded);
    Out += mainLightShaded;

    #ifdef _ADDITIONAL_LIGHTS
        uint pixelLightCount = GetAdditionalLightsCount();

        #if USE_FORWARD_PLUS
        uint meshRenderingLayers = GetMeshRenderingLayer();
        for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
        {
            FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK
            Light light = GetAdditionalLight(lightIndex, positionWS, shadowMask);
            #ifdef _LIGHT_LAYERS
            if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
                #endif
            {
                LightStruct additionalLight;
                Light light = GetAdditionalLight(lightIndex, positionWS, shadowMask);
                additionalLight.color = light.color;
                additionalLight.direction = light.direction;
                additionalLight.distanceAttenuation = light.distanceAttenuation;
                additionalLight.shadowAttenuation = light.shadowAttenuation;
                additionalLight.layerMask = light.layerMask;
                half3 additionalShaded = 0;
                ToonShading(additionalLight, shadowColor, diffuse, positionWS, normalWS, viewDirectionWS, UV, screenUV, diffuseStep, rampLighting, rampTex, state, additionalShaded);
                Out += additionalShaded;
            }
        }
        #endif
        InputData inputData = (InputData)0;
        float4 screenPos = ComputeScreenPos(TransformWorldToHClip(positionWS));
        inputData.normalizedScreenSpaceUV = screenPos.xy / screenPos.w;
        inputData.positionWS = positionWS;
    
        LIGHT_LOOP_BEGIN(pixelLightCount)
            LightStruct additionalLight;
            Light light = GetAdditionalLight(lightIndex, positionWS, shadowMask);
            additionalLight.color = light.color;
            additionalLight.direction = light.direction;
            additionalLight.distanceAttenuation = light.distanceAttenuation;
            additionalLight.shadowAttenuation = light.shadowAttenuation;
            additionalLight.layerMask = light.layerMask;
            half3 additionalShaded = 0;
            ToonShading(additionalLight, shadowColor, diffuse, positionWS, normalWS, viewDirectionWS, UV, screenUV, diffuseStep, rampLighting, rampTex, state, additionalShaded);
            Out += additionalShaded;
        LIGHT_LOOP_END
    #endif
}


void RampLighting_float(half4 shadowColor, half3 GI, half3 diffuse, half3 positionWS, half3 normalWS,
    half3 viewDirectionWS, float2 UV, float2 screenUV, half diffuseStep, half rampLighting,
    Texture2D rampTex, SamplerState state, half4 shadowMask, out half3 Out)
{
    LightStruct mainLight;
    MainLight_float(positionWS, mainLight.direction, mainLight.color, mainLight.distanceAttenuation, mainLight.shadowAttenuation);
    mainLight.shadowAttenuation = saturate(mainLight.shadowAttenuation);
    
    Out = GI;
    half3 mainLightShaded = 0;
    ToonShading(mainLight, shadowColor, diffuse, positionWS, normalWS, viewDirectionWS, UV, screenUV, diffuseStep,
        rampLighting, rampTex, state, mainLightShaded);
    Out += mainLightShaded;

    #ifdef _ADDITIONAL_LIGHTS
        uint pixelLightCount = GetAdditionalLightsCount();
    
        #if USE_FORWARD_PLUS
        uint meshRenderingLayers = GetMeshRenderingLayer();
        for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
        {
            FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK
            Light light = GetAdditionalLight(lightIndex, positionWS, shadowMask);
            #ifdef _LIGHT_LAYERS
            if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
                #endif
            {
                LightStruct additionalLight;
                Light light = GetAdditionalLight(lightIndex, positionWS, shadowMask);
                additionalLight.color = light.color;
                additionalLight.direction = light.direction;
                additionalLight.distanceAttenuation = light.distanceAttenuation;
                additionalLight.shadowAttenuation = light.shadowAttenuation;
                additionalLight.layerMask = light.layerMask;
                half3 additionalShaded = 0;
                ToonShading(additionalLight, shadowColor, diffuse, positionWS, normalWS, viewDirectionWS, UV, screenUV, diffuseStep, rampLighting, rampTex, state, additionalShaded);
                Out += additionalShaded;
            }
        }
        #endif
        InputData inputData = (InputData)0;
        float4 screenPos = ComputeScreenPos(TransformWorldToHClip(positionWS));
        inputData.normalizedScreenSpaceUV = screenPos.xy / screenPos.w;
        inputData.positionWS = positionWS;
    

        LIGHT_LOOP_BEGIN(pixelLightCount)
            LightStruct additionalLight;
            Light light = GetAdditionalLight(lightIndex, positionWS, shadowMask);
            additionalLight.color = light.color;
            additionalLight.direction = light.direction;
            additionalLight.distanceAttenuation = light.distanceAttenuation;
            additionalLight.shadowAttenuation = light.shadowAttenuation;
            additionalLight.layerMask = light.layerMask;
            half3 additionalShaded = 0;
            ToonShading(additionalLight, shadowColor, diffuse, positionWS, normalWS, viewDirectionWS, UV, screenUV, diffuseStep, rampLighting, rampTex, state, additionalShaded);
            Out += additionalShaded;
        LIGHT_LOOP_END
    #endif
}
