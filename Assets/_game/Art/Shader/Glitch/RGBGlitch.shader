Shader "Custom/RGBGlitch"
{
    Properties
    {
        _Intensity    ("Intensity", Float) = 0.004
        _FpsStep      ("FPS Step", Float) = 12
        _GlitchChance ("Glitch Chance", Range(0, 0.1)) = 0.02
        _BandCount    ("Band Count", Float) = 20
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always

        Pass
        {
            Name "RGBGlitch"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Intensity, _FpsStep, _GlitchChance, _BandCount;

            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float t = floor(_Time.y * _FpsStep);

                float active = step(1.0 - _GlitchChance, Hash(float2(t, 1.0)));

                float band = floor(uv.y * _BandCount);
                float r = Hash(float2(band, t)) * 2.0 - 1.0;

                float2 offset = float2(r, r * 0.3) * _Intensity * active;

                half R = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + offset).r;
                half G = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv).g;
                half B = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv - offset).b;

                return half4(R, G, B, 1);
            }
            ENDHLSL
        }
    }
}
