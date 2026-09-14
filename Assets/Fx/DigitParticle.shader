Shader "MadSlime/DigitParticle"
{
    Properties
    {
        _MainTex ("Digit Atlas", 2D) = "white" {}
        _TilesX ("Tiles X", Float) = 11
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "DisableBatching" = "True"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _TilesX;

            struct appdata_t
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 custom : TEXCOORD1;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;

                float frame = clamp(v.custom.x, 0, _TilesX - 1);
                o.texcoord = float2((frame + v.texcoord.x) / _TilesX, v.texcoord.y);

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return i.color * tex2D(_MainTex, i.texcoord);
            }
            ENDCG
        }
    }
}
