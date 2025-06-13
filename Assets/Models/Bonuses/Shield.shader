Shader "Unlit/MobileShieldShader"
{
    Properties
    {
        _Color ("Shield Color", Color) = (1,1,1,0.5) // Основной цвет щита и его прозрачность
        _StripeColor ("Stripe Color", Color) = (0,0,1,1) // Цвет полос
        _StripeFrequency ("Stripe Frequency", Range(1, 50)) = 10 // Количество полос
        _StripeSpeed ("Stripe Speed", Range(0, 10)) = 1 // Скорость движения полос
        _StripeWidth ("Stripe Width", Range(0.01, 0.5)) = 0.1 // Ширина полос
        _NoiseTexture ("Noise Texture (Optional)", 2D) = "white" {} // Для добавления шума в полосы
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" } // Указываем, что объект прозрачный и должен рендериться после непрозрачных

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha // Стандартное смешивание для прозрачности
            ZWrite Off // Отключаем запись в Z-буфер для правильного рендеринга прозрачности
            Cull Off // Отключаем отсечение граней, если щит двусторонний

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma prefer_hlslcc gles // Оптимизация для мобильных устройств (OpenGL ES)
            #pragma target 2.0 // Минимальная версия шейдерной модели для лучшей совместимости

            #include "UnityCG.cginc" // Включаем базовые функции Unity

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

            fixed4 _Color;
            fixed4 _StripeColor;
            float _StripeFrequency;
            float _StripeSpeed;
            float _StripeWidth;
            sampler2D _NoiseTexture;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex); // Преобразование координат вершины в пространство отсечения
                o.uv = v.uv; // Передаем UV-координаты во фрагментный шейдер
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Вычисляем позицию для полос. Используем мировые координаты или UV.
                // Для простоты и равномерности полос по объекту, используем UV-координаты.
                // Вы можете попробовать использовать worldPos.y или worldPos.x для полос в мировом пространстве.
                float stripePos = i.uv.y * _StripeFrequency + _Time.y * _StripeSpeed;

                // Создаем синусоидальную волну для плавного перехода полос
                // float stripeValue = sin(stripePos);
                // float stripeAlpha = smoothstep(1.0 - _StripeWidth, 1.0, abs(stripeValue)); // Для более резких полос

                // Альтернативный способ для более четких полос (например, пилообразная волна)
                float stripeValue = frac(stripePos); // Дробная часть, создает повторяющийся паттерн от 0 до 1
                float stripeMask = step(1.0 - _StripeWidth, stripeValue); // Создаем маску для полосы

                // Добавляем шум из текстуры (необязательно)
                // fixed noise = tex2D(_NoiseTexture, i.uv).r;
                // stripeMask *= (1.0 - noise * 0.5); // Уменьшаем прозрачность полос на основе шума

                fixed4 finalColor = _Color;
                finalColor.rgb = lerp(finalColor.rgb, _StripeColor.rgb, stripeMask * _StripeColor.a);
                finalColor.a = _Color.a * (1.0 - stripeMask * _StripeColor.a); // Прозрачность базового цвета уменьшается, когда полоса активна

                return finalColor;
            }
            ENDHLSL
        }
    }
}