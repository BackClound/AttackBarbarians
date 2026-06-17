#ifndef SKILL_LIGHTNING_LINK_INCLUDED
#define SKILL_LIGHTNING_LINK_INCLUDED

CBUFFER_START(UnityPerMaterial)
    half4 _Color;
    half4 _CoreColor;
    half _Intensity;
    half _Thickness;
    half _Jitter;
    half _FbmScale;
    half _FbmSpeed;
    half _BoltCount;
    half _FlickerSpeed;
    half _StartTime;
    half _Fade;
CBUFFER_END

struct LightningAttributes
{
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
};

struct LightningVaryings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
};

float LightningHash12(float2 p)
{
    return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
}

float2 LightningHash22(float2 p)
{
    p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
    return frac(sin(p) * 43758.5453) * 2.0 - 1.0;
}

float LightningGradientNoise(float2 uv)
{
    float2 i = floor(uv);
    float2 f = frac(uv);
    float2 u = f * f * (3.0 - 2.0 * f);

    float a = dot(LightningHash22(i + float2(0.0, 0.0)), f - float2(0.0, 0.0));
    float b = dot(LightningHash22(i + float2(1.0, 0.0)), f - float2(1.0, 0.0));
    float c = dot(LightningHash22(i + float2(0.0, 1.0)), f - float2(0.0, 1.0));
    float d = dot(LightningHash22(i + float2(1.0, 1.0)), f - float2(1.0, 1.0));

    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y) * 0.5 + 0.5;
}

float LightningFbm(float2 uv, int octaves)
{
    float value = 0.0;
    float amplitude = 0.5;
    float frequency = 1.0;
    for (int i = 0; i < octaves; i++)
    {
        value += amplitude * LightningGradientNoise(uv * frequency);
        frequency *= 2.0;
        amplitude *= 0.5;
    }

    return value;
}

float LightningBoltPath(float along, float t, float layerSeed)
{
    float2 fbmUv = float2(along * _FbmScale + layerSeed, layerSeed * 0.37) + t * _FbmSpeed;
    float path = LightningFbm(fbmUv, 6) - 0.5;
    path += (LightningFbm(fbmUv * 2.1 + 3.7, 3) - 0.5) * 0.5;
    return path * _Jitter;
}

float LightningSampleBolt(float across, float pathX, float thickness)
{
    float dist = abs(across - pathX);
    float core = 1.0 / (dist / max(thickness, 0.01) + 0.04);
    float glow = exp(-dist / max(thickness * 1.8, 0.01));
    return saturate(core * 0.22 + glow * 0.85);
}

LightningVaryings LightningVert(LightningAttributes input)
{
    LightningVaryings output;
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.uv = input.uv;
    return output;
}

half4 LightningFrag(LightningVaryings input) : SV_Target
{
    float along = input.uv.y;
    float across = input.uv.x - 0.5;
    float t = _Time.y - _StartTime;

    float endFade = smoothstep(0.0, 0.03, along) * (1.0 - smoothstep(0.97, 1.0, along));
    float flicker = lerp(0.55, 1.0, LightningHash12(float2(floor(t * _FlickerSpeed), 0.0)));
    flicker *= 0.8 + 0.2 * sin(t * _FlickerSpeed * 2.0);

    float electric = 0.0;
    half3 rgb = half3(0.0, 0.0, 0.0);

    int layers = (int)clamp(round(_BoltCount), 1.0, 5.0);
    for (int i = 0; i < 5; i++)
    {
        if (i >= layers)
        {
            break;
        }

        float seed = (float)i * 5.17 + 1.3;
        float pathX = LightningBoltPath(along, t, seed);
        float layerThickness = _Thickness * lerp(0.8, 1.2, LightningHash12(float2(seed, 2.0)));
        float bolt = LightningSampleBolt(across, pathX, layerThickness);
        if (i > 0)
        {
            bolt *= 0.6;
        }

        electric += bolt;
        rgb += lerp(_Color.rgb, _CoreColor.rgb, saturate(bolt * 2.5)) * bolt;
    }

    electric = saturate(electric);
    rgb *= _Intensity * flicker;
    float alpha = saturate(electric * endFade * _Fade * flicker);

    return half4(rgb, alpha * _Color.a);
}

#endif
