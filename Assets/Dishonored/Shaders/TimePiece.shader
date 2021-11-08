// https://github.com/Broxxar/ACrackInTheSlab/blob/master/Assets/Shaders/TimePiece.shader
Shader "Custom/TimePiece"
{
		SubShader
		{
			Tags { "RenderType" = "Opaque" }
			LOD 100

			Pass
			{
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag

				#include "UnityCG.cginc"
				#include "UnityStandardUtils.cginc"

				struct VertIn
				{
					float4 vertex : POSITION;
					float2 uv : TEXCOORD0;
				};

				struct FragIn
				{
					float4 vertex : SV_POSITION;
					float2 uv : TEXCOORD0;
					float3 screen_uv : TEXCOORD1;
				};

				FragIn vert(VertIn v)
				{
					FragIn o;
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.uv = v.uv;
					o.screen_uv = float3((o.vertex.xy + o.vertex.w) * 0.5, o.vertex.w);
					return o;
				}

				sampler2D _TimePieceTexture;

				fixed4 frag(FragIn i) : SV_Target
				{
					// Perspective correction for screen uv coordinate
					float2 screen_uv = (i.screen_uv.xy / i.screen_uv.z);
					screen_uv.y = 1 - screen_uv.y;
					return tex2D(_TimePieceTexture, screen_uv);
				}
				ENDCG
			}
		}
}
