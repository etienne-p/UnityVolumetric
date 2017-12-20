Shader "Volumetric/BoundingBox"
{
	SubShader
	{
		// Disabling batching is essential here as we want to get untransfromed vertices
		Tags { "DisableBatching"="True"}

		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float3 model_vertex : CUSTOM;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.model_vertex = v.vertex.xyz;
				o.vertex = UnityObjectToClipPos(v.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// offset tied to unity default cube vertices
				return float4(i.model_vertex + float3(.5, .5, .5), 1.0);
			}
			ENDCG
		}
	}
}
