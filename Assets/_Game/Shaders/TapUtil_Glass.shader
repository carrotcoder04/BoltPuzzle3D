// Kính matcap (URP). Viết lại gần đúng từ shader gốc "TapUtil/Glass" (bản AssetRipper chỉ là shader giả).
Shader "TapUtil/Glass"
{
	Properties
	{
		_Color ("Main Color", Color) = (1,1,1,1)
		_NormalTex ("NormalTex", 2D) = "bump" {}
		[HDR] _SpecularColor ("SpecularColor", Color) = (1,1,1,1)
		_MatCapTex ("MapCapTex", 2D) = "white" {}
		_RefMatCapTex ("RefMapCapTex", 2D) = "black" {}
		_ThicknessTex ("Thickness", 2D) = "white" {}
		_ReflectionIntensity ("_ReflectionIntensity", Range(0, 1)) = 0.1
		_EdgeThickness ("Edge Thickness", Range(0, 1)) = 0.2
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

			TEXTURE2D(_MatCapTex); SAMPLER(sampler_MatCapTex);
			TEXTURE2D(_RefMatCapTex); SAMPLER(sampler_RefMatCapTex);
			CBUFFER_START(UnityPerMaterial)
				half4 _Color;
				half4 _SpecularColor;
				half _ReflectionIntensity;
				half _EdgeThickness;
			CBUFFER_END

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float3 normalWS : TEXCOORD0;
				float3 positionWS : TEXCOORD1;
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
				return o;
			}

			half4 frag(Varyings i) : SV_Target
			{
				half3 n = normalize(i.normalWS);
				half3 v = GetWorldSpaceNormalizeViewDir(i.positionWS);
				float2 muv = TransformWorldToViewDir(n, true).xy * 0.5 + 0.5;
				half3 mc = SAMPLE_TEXTURE2D(_MatCapTex, sampler_MatCapTex, muv).rgb;
				half3 rc = SAMPLE_TEXTURE2D(_RefMatCapTex, sampler_RefMatCapTex, muv).rgb;
				half fresnel = pow(1.0 - saturate(dot(n, v)), 2.0);
				half edge = saturate(fresnel * (0.5 + _EdgeThickness));

				half3 col = _Color.rgb * (0.55 + 0.45 * mc) + rc * _ReflectionIntensity + _SpecularColor.rgb * mc * mc * 0.5;
				half refLum = dot(rc, half3(0.299, 0.587, 0.114));
				half alpha = saturate(_Color.a * (0.45 + 0.4 * edge) + refLum * _ReflectionIntensity * 0.5 + edge * 0.3);
				return half4(col, alpha);
			}
			ENDHLSL
		}
	}
	Fallback Off
}
