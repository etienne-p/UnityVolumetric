Shader "Volumetric/Raymarch"
{
	SubShader
	{
		Pass
		{
			Cull Off 
			ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha

			Stencil
			{
				Ref 8
				Comp Equal
			}

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile __ DEPTH_ON
			#pragma multi_compile RES_64 RES_128 RES_256 RES_512

			#include "UnityCG.cginc"

			#define SQRT_2 1.414213562373095

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			sampler2D _FrontBoundsTex;
			sampler2D _BackBoundsTex;
			sampler3D _VolumeTex;
			float4x4 _VolumeModelMatrix;
			float4x4 _CamViewProjectionMatrix;
			float _AlphaMul;

			#if DEPTH_ON
			sampler2D_float _CameraDepthTexture;
			#endif

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			#if RES_64
			#define _Iterations 64
			#elif RES_128
			#define _Iterations 128
			#elif RES_256
			#define _Iterations 256
			#elif RES_512
			#define _Iterations 512
			#endif // deliberately no #else, we want an error if no resolution is defined

			float4 Raymarch(float2 uv)
			{
				#if DEPTH_ON
				float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
				float frameDepth = LinearEyeDepth(rawDepth);
				#endif

			    float4 frontTex = tex2D(_FrontBoundsTex, uv);
			    float4 backTex = tex2D(_BackBoundsTex, uv);

				float3 diff = backTex.xyz - frontTex.xyz;
			    float3 dir = normalize(diff);
			    			 
			    float3 color = float3(0, 0, 0);
				float alpha = 0;

				// didnt want to call this one "step" as it is a built in function
				const float footStep = length(diff) / (_Iterations - 1.0);
				const float maxStep = SQRT_2 / (_Iterations - 1.0);

			    for(int i = 0; i != _Iterations; ++i)
			    {
			    	float3 localPos = frontTex.xyz + dir * footStep * (_Iterations - 1 - i);

			    	#if DEPTH_ON
			    	float4 clipPos = mul(_CamViewProjectionMatrix, mul(_VolumeModelMatrix, float4(localPos, 1)));
			    	float depthFactor = 1 - step(frameDepth, clipPos.w);
			        float4 src = tex3D(_VolumeTex, localPos) * depthFactor;
			        #else
			  		float4 src = tex3D(_VolumeTex, localPos);
			  		#endif
			
			        // as we work with a ray dependent step,
			        // we modulate sample contribution based on step size
			        src.a *= length(src.rgb) * _AlphaMul * footStep / maxStep; 
			        // Back to front blending
					color = src.rgb + (1.0 - src.a) * color;
					//color = color + max(0, (1.0 - length(color))) * src.rgb;
					alpha = src.a + (1.0 - src.a) * alpha;
			    }
			    return float4(color, alpha);
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				return Raymarch(i.uv);
			}
			ENDCG
		}
	}
}
