Shader "MadSlime/AdrenalineVignette"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Vignette Color", Color) = (1, 0.12, 0.08, 1)
        _EdgeStart ("Edge Start", Range(0, 1)) = 0.5
        _EdgeSmoothness ("Edge Smoothness", Range(0.01, 1)) = 0.5
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _EdgeStart;
            float _EdgeSmoothness;

            struct AppData
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct FragmentData
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            FragmentData vert(AppData vertexData)
            {
                FragmentData output;

                output.vertex = UnityObjectToClipPos(vertexData.vertex);
                output.color = vertexData.color;
                output.texcoord = vertexData.texcoord;

                return output;
            }

            fixed4 frag(FragmentData fragmentData) : SV_Target
            {
                float2 centered = abs(fragmentData.texcoord - 0.5) * 2.0;
                float edgeDistance = max(centered.x, centered.y);
                float glow = smoothstep(_EdgeStart, _EdgeStart + _EdgeSmoothness, edgeDistance);

                fixed4 color = _Color;
                color.a *= glow * fragmentData.color.a;

                return color;
            }
            ENDCG
        }
    }
}
