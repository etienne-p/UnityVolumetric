Shader "Volumetric/Normal"
{
	SubShader
	{
		Cull Off
		Zwrite On
		ZTest Less

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float4 normal : NORMAL;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float4 normal : NORMAL;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.normal = normalize(mul( UNITY_MATRIX_IT_MV, v.normal));
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				return float4((float3(i.normal.xyz) + float3(1, 1, 1)) * 0.5, 1.0);
			}
			ENDCG
		}
	}
}
