Shader "UI/GradientUI" {
	Properties {
		_ColorTop ("Top", Vector) = (1,0,0,1)
		[Toggle(_Enable1)] _Enable1 ("Enable 1", Float) = 0
		_Color1 ("Color 1", Vector) = (1,1,1,1)
		_Color1Pos ("Range", Range(0, 1)) = 0
		[Toggle(_Enable2)] _Enable2 ("Enable 2", Float) = 0
		_Color2 ("Color 2", Vector) = (1,1,1,1)
		_Color2Pos ("Range", Range(0, 1)) = 0
		_ColorBottom ("Bottom", Vector) = (0,0,1,1)
		_MainTex ("Sprite", 2D) = "white" {}
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType"="Opaque" }
		LOD 200

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			float4x4 unity_ObjectToWorld;
			float4x4 unity_MatrixVP;
			float4 _MainTex_ST;

			struct Vertex_Stage_Input
			{
				float4 pos : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Vertex_Stage_Output
			{
				float2 uv : TEXCOORD0;
				float4 pos : SV_POSITION;
			};

			Vertex_Stage_Output vert(Vertex_Stage_Input input)
			{
				Vertex_Stage_Output output;
				output.uv = (input.uv.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
				output.pos = mul(unity_MatrixVP, mul(unity_ObjectToWorld, input.pos));
				return output;
			}

			Texture2D<float4> _MainTex;
			SamplerState sampler_MainTex;

			struct Fragment_Stage_Input
			{
				float2 uv : TEXCOORD0;
			};

			float4 frag(Fragment_Stage_Input input) : SV_TARGET
			{
				return _MainTex.Sample(sampler_MainTex, input.uv.xy);
			}

			ENDHLSL
		}
	}
}