// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Volumetric/Stencil"
{
	SubShader
	{
		Pass
		{
			ColorMask 0 
			ZTest Less
			ZWrite Off

			Stencil
			{
				Ref 8
				Comp Always
				Pass Replace
			}

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
		
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				return float4(1, 0, 0, 1);
			}
			ENDCG
		}
	}
}
