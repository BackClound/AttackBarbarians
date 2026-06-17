Shader "AttackBarbarians/SkillLightningLink"
{
    Properties
    {
        [HDR] _Color ("Outer Glow", Color) = (0.2, 0.4, 1, 1)
        [HDR] _CoreColor ("Core", Color) = (0.9, 0.7, 1, 1)
        _Intensity ("Intensity", Range(0, 12)) = 5
        _Thickness ("Thickness", Range(0.01, 0.3)) = 0.08
        _Jitter ("Path Jitter", Range(0, 1)) = 0.42
        _FbmScale ("Fbm Scale", Float) = 9
        _FbmSpeed ("Fbm Speed", Float) = 1.6
        _BoltCount ("Bolt Layers", Range(1, 5)) = 3
        _FlickerSpeed ("Flicker Speed", Float) = 16
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
            #include "SkillLightningLinkPass.hlsl"

            LightningVaryings vert(LightningAttributes input)
            {
                return LightningVert(input);
            }

            half4 frag(LightningVaryings input) : SV_Target
            {
                return LightningFrag(input);
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
            #include "SkillLightningLinkPass.hlsl"

            LightningVaryings vert(LightningAttributes input)
            {
                return LightningVert(input);
            }

            half4 frag(LightningVaryings input) : SV_Target
            {
                return LightningFrag(input);
            }
            ENDHLSL
        }
    }

    FallBack "Sprites/Default"
}
