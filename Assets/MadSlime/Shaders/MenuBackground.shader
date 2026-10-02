Shader "MadSlime/MenuBackground"
{
    // Фон главного меню: базовая картинка + тайлящийся слой звёзд.
    // Картинка фона кладётся в Texture самого RawImage (шейдер читает её
    // как _MainTex — это контракт RawImage, переименовывать нельзя),
    // картинка звезды — в _OverlayTexture материала.
    // Слой замощён _OverlayTiling повторами и плывёт со скоростью
    // _OverlayScroll (в тайлах/сек), направление — знак XY.
    // Каждая звезда в своём тайле вращается вокруг центра: статичный
    // наклон _OverlayRotation плюс _OverlaySpinSpeed градусов в секунду
    // (минус — в обратную сторону). Размер звезды — _OverlayScale:
    // 1 = звезда занимает весь тайл, меньше — меньше звезда с воздухом
    // вокруг (край текстуры должен быть прозрачным, иначе будут потёки).
    // Время приходит через _UnscaledTime — его каждый кадр пишет
    // MenuBackground.cs, поэтому фон не замирает при Time.timeScale = 0.
    Properties
    {
        _MainTex ("Background Texture", 2D) = "white" {}
        _OverlayTexture ("Moving Layer Texture", 2D) = "black" {}
        _OverlayColor ("Moving Layer Tint", Color) = (1, 1, 1, 1)
        _OverlayTiling ("Moving Layer Tiling (XY = repeats)", Vector) = (8, 8, 0, 0)
        _OverlayScale ("Star Size (1 = fills tile)", Range(0.05, 2)) = 1
        _OverlayRotation ("Star Rotation (degrees)", Range(0, 360)) = 0
        _OverlaySpinSpeed ("Star Spin Speed (degrees/sec, minus = left)", Float) = 0
        _OverlayScroll ("Moving Layer Scroll (XY = tiles/sec)", Vector) = (0.02, 0.012, 0, 0)
        _UnscaledTime ("Unscaled Time", Float) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane" }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _OverlayTexture;
            fixed4 _OverlayColor;
            float4 _OverlayTiling;
            float _OverlayScale;
            float _OverlayRotation;
            float _OverlaySpinSpeed;
            float4 _OverlayScroll;
            float _UnscaledTime;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            v2f vert(appdata input)
            {
                v2f output;

                output.position = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                output.color = input.color;

                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 background = tex2D(_MainTex, input.uv) * input.color;

                // Координаты внутри своего тайла: каждая звезда вращается
                // вокруг центра тайла независимо от соседей, вся решётка
                // при этом плывёт скроллом как целое.
                float2 tiledCoordinates = input.uv * _OverlayTiling.xy + _OverlayScroll.xy * _UnscaledTime;
                float2 withinTile = tiledCoordinates - floor(tiledCoordinates);

                float starAngle = radians(_OverlayRotation + _OverlaySpinSpeed * _UnscaledTime);
                float angleSin;
                float angleCos;

                sincos(starAngle, angleSin, angleCos);

                float2 centered = withinTile - 0.5;
                float2 rotated = float2(
                    centered.x * angleCos - centered.y * angleSin,
                    centered.x * angleSin + centered.y * angleCos);

                // Размер: 1 = звезда на весь тайл, меньше — воздух вокруг.
                // saturate гасит выход за пределы текстуры прозрачным краем.
                float2 sampled = saturate(rotated / max(_OverlayScale, 0.0001) + 0.5);
                fixed4 overlay = tex2D(_OverlayTexture, sampled) * _OverlayColor;

                background.rgb = lerp(background.rgb, overlay.rgb, overlay.a);

                return background;
            }
            ENDCG
        }
    }

    Fallback Off
}
