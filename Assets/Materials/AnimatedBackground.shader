// Название шейдера и путь в инспекторе Unity
Shader "Universal Render Pipeline/Unlit/AnimatedGridObject"
{
    // Свойства, настраиваемые из Unity Editor
    Properties
    {
        [Header(Grid Settings)]
        _Color1 ("Цвет ячейки 1", Color) = (0.8, 0.9, 1.0, 1.0)
        _Color2 ("Цвет ячейки 2", Color) = (0.6, 0.7, 0.9, 1.0)
        _GridSize ("Размер сетки (Tiling)", Range(1.0, 100.0)) = 10.0 // Плотность сетки на UV-пространстве
        
        [Header(Animation)]
        _SpeedX ("Скорость анимации по X", Range(-5.0, 5.0)) = 0.5
        _SpeedY ("Скорость анимации по Y", Range(-5.0, 5.0)) = 0.3
        _OffsetX ("Смещение узора по X", Range(-5.0, 5.0)) = 0.0
        _OffsetY ("Смещение узора по Y", Range(-5.0, 5.0)) = 0.0

        [Header(Pulse Effect)]
        _PulseSpeed ("Скорость пульсации", Range(0.0, 5.0)) = 1.0
        _BrightnessVariation ("Изменение яркости пульсации", Range(0.0, 0.5)) = 0.1

        [Header(Center Glow Effect)]
        _CenterBrightnessFactor ("Множитель яркости центра", Range(1.0, 3.0)) = 1.5 // Насколько ярче центр UV
        _CenterFalloffStart ("Начало спада яркости (0-1)", Range(0.0, 1.0)) = 0.1 // Дистанция от UV-центра, где начинается спад
        _CenterFalloffEnd ("Конец спада яркости (0-1)", Range(0.0, 1.0)) = 0.5   // Дистанция от UV-центра, где спад завершается
    }

    SubShader
    {
        // Теги для URP, указывающие, что это непрозрачный шейдер для стандартного рендера
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalRenderPipeline" "Queue" = "Geometry" }

        Pass
        {
            // Начало блока HLSL кода для URP
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // Подключение заголовочных файлов URP
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Структура входных данных вершин (из меша объекта)
            struct Attributes
            {
                float4 positionOS   : POSITION;     // Позиция вершины в локальном пространстве объекта
                float2 uv           : TEXCOORD0;    // UV-координаты из меша
            };

            // Структура для передачи данных из вершинного в фрагментный шейдер
            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;  // Позиция вершины в пространстве клиппинга (обязательно для вывода)
                float2 uv           : TEXCOORD0;    // UV-координаты для отрисовки сетки
            };

            // CBUFFER содержит все свойства, объявленные в блоке Properties
            CBUFFER_START(UnityPerMaterial)
                half4 _Color1;
                half4 _Color2;
                float _GridSize;
                float _SpeedX;
                float _SpeedY;
                float _OffsetX;
                float _OffsetY;
                float _PulseSpeed;
                float _BrightnessVariation;
                float _CenterBrightnessFactor;
                float _CenterFalloffStart;
                float _CenterFalloffEnd;
            CBUFFER_END

            // Вершинный шейдер
            Varyings vert(Attributes i)
            {
                Varyings o;
                // Трансформируем позицию вершины из локального пространства в пространство клиппинга
                o.positionHCS = TransformObjectToHClip(i.positionOS.xyz);
                // Просто передаем UV-координаты меша дальше
                o.uv = i.uv;
                return o;
            }

            // Фрагментный шейдер
            half4 frag(Varyings i) : SV_Target
            {
                // --- Анимированная сетка ---
                float t = _Time.y;
                float2 offset = float2(_OffsetX + _SpeedX * t, _OffsetY + _SpeedY * t);
                float2 uvTile = i.uv * _GridSize;
                float2 animatedUV = uvTile + offset;

                float checker = floor(animatedUV.x) + floor(animatedUV.y);
                checker = fmod(checker, 2.0); // Чередование 0 и 1

                half4 finalColor = (checker < 0.5) ? _Color1 : _Color2;

                // --- Пульсация яркости ---
                float s = sin(t * _PulseSpeed);
                finalColor.rgb *= (1.0 - _BrightnessVariation + s * _BrightnessVariation);
                
                // --- Эффект осветления центра ---
                float centerFade = smoothstep(_CenterFalloffEnd, _CenterFalloffStart, length(i.uv - 0.5) * 2.0);
                finalColor.rgb *= (1.0 + (_CenterBrightnessFactor - 1.0) * centerFade);

                return finalColor;
            }
            ENDHLSL
        }
    }
}
