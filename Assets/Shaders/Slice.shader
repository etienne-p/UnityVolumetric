Shader "Volumetric/Slice"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Pass
		{
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
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_TexelSize;
			float _Step;

			float3 DecodeNormal(float2 uv, float2 offset)
			{
				return tex2D(_MainTex, uv + _MainTex_TexelSize.xy * offset).xyz * 2 - float3(1, 1, 1);
			}

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			float sqrLength(float3 v)
			{
				return dot(v, v);
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float3 norm = normalize(DecodeNormal(i.uv, float2( 0,  0)));

				float grad = 0.5 * (
					sqrLength(DecodeNormal(i.uv, float2(1, 0)) - DecodeNormal(i.uv, float2(-1,  0))) +
					sqrLength(DecodeNormal(i.uv, float2(0, 1)) - DecodeNormal(i.uv, float2( 0, -1))));

				float inside = dot(norm, float3(0, 0, -1)) * length(tex2D(_MainTex, i.uv).rgb);

				float v = step(_Step, inside - grad);

				return float4(v, v, v, 1);
			}
			ENDCG
		}
	}
}
