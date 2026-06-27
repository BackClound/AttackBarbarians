#ifndef COPY_LIGHTNING_PASS_INCLUDED
#define COPY_LIGHTNING_PASS_INCLUDED

// 复刻 B 站「小猫学游戏」2D 简单闪电 Shader Graph 思路：
// mod 锯齿路径 + Step/Power 锐化 + 简易噪声扰动 + 端点衰减 + 闪烁。

CBUFFER_START(UnityPerMaterial)
    half4 _Color;
    half4 _CoreColor;
    half _Intensity;
    half _Cycle;
    half _Speed;
    half _BoltSize;
    half _WidthFactor;
    half _NoiseScale;
    half _NoiseStrength;
    half _Power;
    half _FlickerSpeed;
    half _StartTime;
    half _Fade;
CBUFFER_END

struct CopyLightningAttributes
{
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
};

struct CopyLightningVaryings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
};

float CopyHash(float x)
{
    return frac(sin(x) * 100000.0);
}

// 等价 GLSL mod：对负数返回正余数，避免使用 Metal 不支持的 mod()。
float CopyMod(float x, float y)
{
    return x - y * floor(x / y);
}

float CopyHash12(float2 p)
{
    return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
}

float CopyValueNoise(float2 uv)
{
    float2 i = floor(uv);
    float2 f = frac(uv);
    float2 u = f * f * (3.0 - 2.0 * f);
    float a = CopyHash(i.x + i.y * 57.0);
    float b = CopyHash(i.x + 1.0 + i.y * 57.0);
    float c = CopyHash(i.x + (i.y + 1.0) * 57.0);
    float d = CopyHash(i.x + 1.0 + (i.y + 1.0) * 57.0);
    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

float CopyZigZagOffset(float along, float t, float timeShift)
{
    float wave = abs(CopyMod(along * _Cycle + (CopyHash(t + timeShift) + timeShift) * _Speed * -1.0, 0.5) - 0.25) - 0.125;
    wave *= 4.0 * _WidthFactor;
    wave *= (0.5 - abs(along - 0.5)) * 2.0;
    return wave;
}

float CopyBoltMask(float across, float centerX, float size)
{
    float dist = abs(across - centerX);
    float lineMask = 1.0 - smoothstep(size * 0.35, size * 0.5, dist);
    return pow(saturate(lineMask), _Power);
}

CopyLightningVaryings CopyLightningVert(CopyLightningAttributes input)
{
    CopyLightningVaryings output;
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.uv = input.uv;
    return output;
}

half4 CopyLightningFrag(CopyLightningVaryings input) : SV_Target
{
    float along = input.uv.y;
    float across = input.uv.x;
    float t = _Time.y - _StartTime;

    float endFade = smoothstep(0.0, 0.04, along) * (1.0 - smoothstep(0.96, 1.0, along));
    float blink = step(CopyHash(floor(t * _FlickerSpeed)), 0.48);
    blink = lerp(0.45, 1.0, blink);

    float bolt = 0.0;
    half3 rgb = half3(0.0, 0.0, 0.0);

    for (int i = 0; i < 3; i++)
    {
        float layerShift = (float)i * 2.37;
        float offset = CopyZigZagOffset(along, t, layerShift);
        float centerX = 0.5 + offset;
        float layer = CopyBoltMask(across, centerX, _BoltSize);
        if (i > 0)
        {
            layer *= 0.55;
        }

        bolt += layer;
        rgb += lerp(_Color.rgb, _CoreColor.rgb, saturate(layer * 2.0)) * layer;
    }

    float noise = CopyValueNoise(float2(along * _NoiseScale, t * 2.5));
    float noiseLine = step(0.62 - _NoiseStrength, noise) * step(noise, 0.98);
    float noiseCenter = 0.5 + CopyZigZagOffset(along, t, 7.1);
    float noiseBolt = CopyBoltMask(across, noiseCenter, _BoltSize * 0.75) * noiseLine * 0.45;

    bolt = saturate(bolt + noiseBolt);
    rgb += lerp(_Color.rgb, _CoreColor.rgb, 0.5) * noiseBolt;
    rgb *= _Intensity * blink;

    float alpha = saturate(bolt * endFade * _Fade * blink);
    return half4(rgb, alpha * _Color.a);
}

#endif
