// Kính trong suốt cho các tấm Board (URP). Viết lại từ shader gốc "Snebula/Glass" (bản AssetRipper chỉ là shader giả).
Shader "Snebula/Glass"
{
	Properties
	{
		_Color ("Color Tint", Color) = (1,1,1,1)
		_MainTex ("Main Tex", 2D) = "white" {}
		_AlphaScale ("Alpha Scale", Range(0, 1)) = 1
	}
	SubShader
	{
		Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }

		Pass
		{
			Name "ForwardGlass"
			Tags { "LightMode"="UniversalForward" }
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off
			Cull Back

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
			CBUFFER_START(UnityPerMaterial)
				float4 _MainTex_ST;
				half4 _Color;
				half _AlphaScale;
			CBUFFER_END

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
				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings vert(Attributes v)
			{
				Varyings o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
				o.positionCS = TransformWorldToHClip(o.positionWS);
				o.normalWS = TransformObjectToWorldNormal(v.normalOS);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}

			half4 frag(Varyings i) : SV_Target
			{
				half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
				half3 n = normalize(i.normalWS);
				half3 v = GetWorldSpaceNormalizeViewDir(i.positionWS);
				Light light = GetMainLight();
				half ndl = saturate(dot(n, light.direction)) * 0.5 + 0.5;
				half3 h = normalize(light.direction + v);
				half spec = pow(saturate(dot(n, h)), 48.0);
				half fresnel = pow(1.0 - saturate(dot(n, v)), 3.0);

				half3 col = tex.rgb * _Color.rgb * (0.65 + 0.35 * ndl) + spec * 0.6 + fresnel * 0.25;
				half alpha = saturate(tex.a * _Color.a * _AlphaScale + fresnel * 0.35 + spec);
				return half4(col, alpha);
			}
			ENDHLSL
		}
	}
	Fallback Off
}
