Shader "MadSlime/GhostDither"
{
    Properties
    {
        _Color ("Tint", Color) = (0.75, 0.9, 1, 1)
        _Opacity ("Opacity", Range(0, 1)) = 0.5
        _DitherScale ("Dither Scale", Range(0.25, 4)) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }

        Pass
        {
            Tags { "LightMode" = "ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"

            fixed4 _Color;
            half _Opacity;
            half _DitherScale;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float4 screenPosition : TEXCOORD1;
            };

            v2f vert(appdata input)
            {
                v2f output;

                output.position = UnityObjectToClipPos(input.vertex);
                output.worldNormal = UnityObjectToWorldNormal(input.normal);
                output.screenPosition = ComputeScreenPos(output.position);

                return output;
            }

            float Bayer2(float2 pixelPosition)
            {
                float2 cell = floor(pixelPosition);
                return frac(cell.x * 0.5 + cell.y * cell.y * 0.75);
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 pixelPosition = (input.screenPosition.xy / input.screenPosition.w) * _ScreenParams.xy * _DitherScale;

                float threshold = Bayer2(pixelPosition * 0.5) * 0.25 + Bayer2(pixelPosition);

                clip(_Opacity - threshold);

                half3 normal = normalize(input.worldNormal);
                half lambert = saturate(dot(normal, normalize(_WorldSpaceLightPos0.xyz)));
                half3 lighting = lambert * _LightColor0.rgb + UNITY_LIGHTMODEL_AMBIENT.rgb;

                return fixed4(_Color.rgb * lighting, 1);
            }
            ENDCG
        }
    }

    Fallback Off
}
