Shader "MadSlime/ItemOutline"
{
    // Лёгкий аутлайн для подсветки квотовых предметов (перк "Улучшенный нюх").
    // Материал добавляется последним слотом в массив материалов рендерера:
    // меш рисуется второй раз растянутым силуэтом (inverted hull) — предмет
    // сохраняет свои текстуры, вокруг него появляется обводка.
    // Цвет и толщина настраиваются и в самом материале, и через
    // MaterialPropertyBlock из Item.cs (конфиг перка).
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1, 0.85, 0.2, 1)
        _Thickness ("Thickness (Screen Height Fraction)", Range(0, 0.5)) = 0.012
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

            fixed4 _OutlineColor;
            half _Thickness;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
            };

            v2f vert(appdata input)
            {
                v2f output;

                // Классический inverted hull: силуэт раздувается по нормалям,
                // обводка прилегает к мешу при любом пивоте и форме. Смещение в
                // клип-пространстве умножается на w (не делится!): после
                // перспективного деления толщина на экране постоянна и равна
                // _Thickness (доля высоты экрана) на любом расстоянии. Имена
                // переменных — не length/distance: это инстринсики HLSL.
                output.position = UnityObjectToClipPos(input.vertex);

                float3 viewNormal = normalize(mul((float3x3) UNITY_MATRIX_IT_MV, input.normal));
                float3 clipNormal = mul((float3x3) UNITY_MATRIX_P, viewNormal);
                float2 offset = clipNormal.xy;
                float offsetLength = max(length(offset), 1e-5);

                output.position.xy += offset / offsetLength * _Thickness * output.position.w;

                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }
    }

    Fallback Off
}
