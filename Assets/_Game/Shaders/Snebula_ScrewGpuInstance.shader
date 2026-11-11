Shader "Snebula/ScrewGpuInstance" {
	Properties {
		_Color ("Color", Vector) = (1,1,1,1)
		_TexArray ("Texture Array", 2DArray) = "" {}
		_TexIndex ("Texture Index", Float) = 0
		_SpecColo2 ("Specular Color", Vector) = (0.2,0.2,0.2,1)
		_SpecGlossMap ("Specular Gloss Map (RGB=Spec, A=Gloss)", 2D) = "white" {}
		_Shininess ("Shininess", Range(0.01, 1)) = 0.5
		[ToggleOff] _SpecularHighlights ("Specular Highlights", Float) = 1
		[ToggleOff] _GlossyReflections ("Glossy Reflections", Float) = 1
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

			struct Vertex_Stage_Input
			{
				float4 pos : POSITION;
			};

			struct Vertex_Stage_Output
			{
				float4 pos : SV_POSITION;
			};

			Vertex_Stage_Output vert(Vertex_Stage_Input input)
			{
				Vertex_Stage_Output output;
				output.pos = mul(unity_MatrixVP, mul(unity_ObjectToWorld, input.pos));
				return output;
			}

			float4 _Color;

			float4 frag(Vertex_Stage_Output input) : SV_TARGET
			{
				return _Color; // RGBA
			}

			ENDHLSL
		}
	}
	Fallback "StandardSpecular"
}