// Nền gradient dọc 2–4 màu (dùng cho BgPlane trong GameScene). Viết lại từ shader gốc "UI/GradientUI".
Shader "UI/GradientUI"
{
	Properties
	{
		_ColorTop ("Top", Color) = (1,0,0,1)
		[Toggle] _Enable1 ("Enable 1", Float) = 0
		_Color1 ("Color 1", Color) = (1,1,1,1)
		_Color1Pos ("Range", Range(0, 1)) = 0
		[Toggle] _Enable2 ("Enable 2", Float) = 0
		_Color2 ("Color 2", Color) = (1,1,1,1)
		_Color2Pos ("Range", Range(0, 1)) = 0
		_ColorBottom ("Bottom", Color) = (0,0,1,1)
		_MainTex ("Sprite", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }

		Pass
		{
			Name "Gradient"
			Tags { "LightMode"="UniversalForward" }
			Cull Off

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
			CBUFFER_START(UnityPerMaterial)
				float4 _MainTex_ST;
				half4 _ColorTop;
				half4 _Color1;
				half4 _Color2;
				half4 _ColorBottom;
				half _Enable1;
				half _Enable2;
				half _Color1Pos;
				half _Color2Pos;
			CBUFFER_END

			struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
			struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

			Varyings vert(Attributes v)
			{
				Varyings o;
				o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
				o.uv = v.uv;
				return o;
			}

			half4 frag(Varyings i) : SV_Target
			{
				half t = saturate(i.uv.y);
				// Các mốc: đáy(0) → màu1(p1) → màu2(p2) → đỉnh(1); mốc tắt thì trùng với đầu mút.
				half p1 = _Enable1 > 0.5 ? _Color1Pos : 0.0;
				half4 c1 = _Enable1 > 0.5 ? _Color1 : _ColorBottom;
				half p2 = _Enable2 > 0.5 ? max(_Color2Pos, p1) : 1.0;
				half4 c2 = _Enable2 > 0.5 ? _Color2 : _ColorTop;

				half4 c;
				if (t < p1) c = lerp(_ColorBottom, c1, t / max(p1, 1e-4));
				else if (t < p2) c = lerp(c1, c2, (t - p1) / max(p2 - p1, 1e-4));
				else c = lerp(c2, _ColorTop, (t - p2) / max(1.0 - p2, 1e-4));

				half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv * _MainTex_ST.xy + _MainTex_ST.zw);
				return half4(c.rgb * tex.rgb, 1);
			}
			ENDHLSL
		}
	}
	Fallback Off
}
