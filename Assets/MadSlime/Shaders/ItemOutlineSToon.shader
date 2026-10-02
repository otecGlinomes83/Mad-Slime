Shader "MadSlime/ItemOutlineSToon"
{
    Properties
    {
        _OtlColor ("Color", COLOR) = (1, 0.85, 0.2, 1)
        _OtlWidth ("Width", Range(0, 5)) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry+1" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Outline"
            Cull Front
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"

            fixed4 _OtlColor;
            half _OtlWidth;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = v.vertex;
                o.pos.xyz += normalize(v.normal.xyz) * _OtlWidth * 0.008;
                o.pos = UnityObjectToClipPos(o.pos);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                if (_OtlWidth <= 0.0)
                {
                    discard;
                }

                return _OtlColor;
            }
            ENDCG
        }
    }

    Fallback Off
}
