Shader "AttackBarbarians/CopyLightning"
{
    Properties
    {
        [HDR] _Color ("Outer Color", Color) = (0.18, 0.42, 1, 1)
        [HDR] _CoreColor ("Core Color", Color) = (0.88, 0.78, 1, 1)
        _Intensity ("Intensity", Range(0, 12)) = 4.5
        _Cycle ("ZigZag Cycle", Range(2, 20)) = 9
        _Speed ("Anim Speed", Float) = 4
        _BoltSize ("Bolt Size", Range(0.01, 0.2)) = 0.065
        _WidthFactor ("ZigZag Width", Range(0.2, 2)) = 1
        _NoiseScale ("Noise Scale", Float) = 14
        _NoiseStrength ("Noise Strength", Range(0, 0.5)) = 0.12
        _Power ("Edge Power", Range(1, 8)) = 3.5
        _FlickerSpeed ("Flicker Speed", Float) = 14
        _StartTime ("Start Time", Float) = 0
        _Fade ("Fade", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Universal2D"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "CopyLightningPass.hlsl"

            CopyLightningVaryings vert(CopyLightningAttributes input)
            {
                return CopyLightningVert(input);
            }

            half4 frag(CopyLightningVaryings input) : SV_Target
            {
                return CopyLightningFrag(input);
            }
            ENDHLSL
        }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "CopyLightningPass.hlsl"

            CopyLightningVaryings vert(CopyLightningAttributes input)
            {
                return CopyLightningVert(input);
            }

            half4 frag(CopyLightningVaryings input) : SV_Target
            {
                return CopyLightningFrag(input);
            }
            ENDHLSL
        }
    }

    FallBack "Sprites/Default"
}
