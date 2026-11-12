// Shader ốc vít nổi bật (URP): màu tươi hơn, sáng kiểu toon mềm, highlight bóng, viền sáng (rim) và viền ngoài (outline).
// Giữ tên _BaseMap/_BaseColor như SimpleLit nên chỉ cần đổi shader của material, Screw.SetColor vẫn tô màu qua MaterialPropertyBlock.
Shader "BoltPuzzle/ScrewPop"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)
        _Saturation("Saturation", Range(0, 2)) = 1.25
        _Brightness("Brightness", Range(0.5, 2)) = 1.1
        _Ambient("Ambient (shadow side)", Range(0, 1)) = 0.5
        _ShadeSoftness("Shade Softness", Range(0.01, 1)) = 0.45
        [Header(Specular)]
        _SpecColor("Spec Color", Color) = (1,1,1,1)
        _Gloss("Gloss", Range(4, 256)) = 48
        _SpecStrength("Spec Strength", Range(0, 2)) = 0.7
        [Header(Rim)]
        _RimColor("Rim Color", Color) = (1,1,1,1)
        _RimPower("Rim Power", Range(0.5, 8)) = 3
        _RimStrength("Rim Strength", Range(0, 2)) = 0.45
        [Header(Outline)]
        _OutlineColor("Outline Color", Color) = (0.06,0.07,0.16,1)
        _OutlineTint("Outline Tint From Base", Range(0, 1)) = 0.35
        _OutlineWidth("Outline Width (px)", Range(0, 8)) = 2.2
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _Saturation;
            half _Brightness;
            half _Ambient;
            half _ShadeSoftness;
            half4 _SpecColor;
            half _Gloss;
            half _SpecStrength;
            half4 _RimColor;
            half _RimPower;
            half _RimStrength;
            half4 _OutlineColor;
            half _OutlineTint;
            half _OutlineWidth;
        CBUFFER_END

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes input)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                half3 albedo = tex.rgb * _BaseColor.rgb;
                half luma = dot(albedo, half3(0.299h, 0.587h, 0.114h));
                albedo = max(0, lerp(luma.xxx, albedo, _Saturation)) * _Brightness;

                float3 n = normalize(i.normalWS);
                float3 v = normalize(GetWorldSpaceViewDir(i.positionWS));
                Light light = GetMainLight();
                float3 l = normalize(light.direction);

                // sáng/tối kiểu toon mềm: mặt khuất vẫn giữ màu (Ambient) để ốc luôn rõ màu
                half ndl = dot(n, l) * 0.5h + 0.5h;
                half shade = smoothstep(0.5h - _ShadeSoftness * 0.5h, 0.5h + _ShadeSoftness * 0.5h, ndl);
                half diffuse = lerp(_Ambient, 1.0h, shade);

                // highlight bóng gọn (dạng đốm sáng hoạt hình)
                float3 h = normalize(l + v);
                half spec = pow(saturate(dot(n, h)), _Gloss);
                spec = smoothstep(0.35h, 0.6h, spec) * _SpecStrength;

                // viền sáng ở rìa để tách ốc khỏi tấm phía sau
                half rim = pow(1.0h - saturate(dot(n, v)), _RimPower) * _RimStrength;

                half3 lightCol = max(light.color, half3(0.6h, 0.6h, 0.6h));
                half3 color = albedo * diffuse * lightCol
                            + spec * _SpecColor.rgb
                            + rim * _RimColor.rgb * (albedo * 0.5h + 0.5h);
                return half4(color, 1.0h);
            }
            ENDHLSL
        }

        // Viền ngoài: đẩy mặt sau ra theo pháp tuyến trong không gian màn hình nên độ dày tính theo pixel, không phụ thuộc zoom
        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes input)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                float4 pos = TransformObjectToHClip(input.positionOS.xyz);
                float3 nWS = TransformObjectToWorldNormal(input.normalOS);
                float2 nCS = mul((float3x3)UNITY_MATRIX_VP, nWS).xy;
                float len = max(length(nCS), 1e-4);
                pos.xy += (nCS / len) * _OutlineWidth * 2.0 / _ScreenParams.xy * pos.w;
                o.positionCS = pos;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                half3 c = lerp(_OutlineColor.rgb, _BaseColor.rgb * 0.3h, _OutlineTint);
                return half4(c, 1.0h);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/SimpleLit/SHADOWCASTER"
        UsePass "Universal Render Pipeline/SimpleLit/DEPTHONLY"
    }
    FallBack "Universal Render Pipeline/SimpleLit"
}
