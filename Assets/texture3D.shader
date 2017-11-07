// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/texture3D"
{
	Properties
	{
		_Volume ("Volume", 3D) = "" {}
		_Offset ("Offset", float) = 1
		_Zoom ("Zoom", float) = 1
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue"="Transparent"}
		LOD 100

        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct vs_input {
				float4 vertex : POSITION;
				float4 pos : SV_POSITION;
			};
			
			struct ps_input {
				float4 pos : SV_POSITION;
				float3 uv : TEXCOORD0;
			};
			
			sampler3D		_Volume;
			float			_Offset;
			float			_Zoom;
			
			ps_input vert (vs_input v)
			{
				ps_input o;
				o.pos = UnityObjectToClipPos (v.vertex);
				o.uv = mul (unity_ObjectToWorld, v.vertex).xyz;
				return o;
			}
			
			float4 frag (ps_input i) : COLOR
			{
				// return tex3D (_Volume, i.uv);
				return float4(tex3D (_Volume, i.uv * _Zoom).aaaa);
			}

			ENDCG
		}
	}
}
