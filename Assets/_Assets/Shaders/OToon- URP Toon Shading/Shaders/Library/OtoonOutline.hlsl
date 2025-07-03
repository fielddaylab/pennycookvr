#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
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
    half _ToonBlending;
    half _DiffuseStep;
    half _HalfToneUvMode;
    half _SpecularFalloff;
    half _SpecularSize;
    half _HalfToneEnabled;
    half4 _HalfToneColor;
    half _HalftoneNoiseClip;
    half _BrushLowerCut;
    half _BrushSize;
    half _HalftoneTilling;
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

struct VertexInput
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float4 tangent : TANGENT;
    float2 texcoord0 : TEXCOORD0;
};
struct VertexOutput
{
    float4 pos : SV_POSITION;
    float2 uv0 : TEXCOORD0;
    float4 screenPos : TEXCOORD1;
};

float Remap(float In, float2 InMinMax, float2 OutMinMax)
{
    return OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
}

VertexOutput vert(VertexInput v)
{
    VertexOutput o = (VertexOutput)0;
    o.uv0 = v.texcoord0;
    float4 objPos = mul(unity_ObjectToWorld, float4(0, 0, 0, 1));
    VertexPositionInputs vertexInput = GetVertexPositionInputs(v.vertex.xyz);
    float3 normalHCS = mul((float3x3)UNITY_MATRIX_VP, mul((float3x3)UNITY_MATRIX_M, v.normal));
    o.pos = vertexInput.positionCS;
    float outlineWidth = lerp(0, _OutlineWidth, Remap(max(_OutlineDistancFade.y - distance(_WorldSpaceCameraPos, vertexInput.positionWS), 0), float2(0, _OutlineDistancFade.y), float2(0, 1)));
    outlineWidth = lerp(0, outlineWidth, saturate((distance(vertexInput.positionWS, _WorldSpaceCameraPos) - _OutlineDistancFade.x) / _OutlineDistancFade.y));
    o.pos.xy += normalize(normalHCS.xy) / _ScreenParams.xy * o.pos.w * outlineWidth;

    o.screenPos = ComputeScreenPos(o.pos);
    return o;
}

float4 frag(VertexOutput i) : SV_Target
{
    #ifndef _OUTLINE
        clip(-1);
    #endif

    clip(-_OutlineMode);
    return float4(_OutlineColor);
}
