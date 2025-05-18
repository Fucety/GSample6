Shader "Custom/VoronoiAdvanced_ProceduralNoise_Stretch_URP_Omni"
{
    Properties
    {
        _OutputType ("Output Mode", Range(0, 2)) = 0 // 0: Color Ramp, 1: Cell Color, 2: Checkerboard
        _Color1 ("Color 1", Color) = (0.2,0.2,0.2,1)
        _Color2 ("Color 2", Color) = (0.5,0.5,0.5,1)
        _Color3 ("Color 3", Color) = (0.8,0.8,0.8,1)
        _Threshold1 ("Threshold 1 (0-1)", Range(0.01, 0.99)) = 0.33 // Настройте, если цвета отличаются
        _Threshold2 ("Threshold 2 (0-1)", Range(0.01, 0.99)) = 0.66 // Настройте, если цвета отличаются
        _SharpBorders ("Sharp Transitions (0-1)", Range(0, 1)) = 1 // Увеличьте для более резких переходов
        _MetricForRamp ("Metric for Color Ramp", Range(0, 2)) = 0 // 0: F1 Euclidean, 1: F2 Euclidean, 2: Manhattan
        _VoronoiScale ("Voronoi Scale", Range(0.1, 100)) = 10.0 // Настройте, если размер клеток отличается
        _VoronoiJitter ("Voronoi Jitter", Range(0, 1)) = 0.9
        [Header(Mapping)]
        _UVMappingMode ("UV Mapping Mode", Range(0, 1)) = 0 // 0: UV, 1: Planar
        _PlanarStrength("Planar Strength", Range(0.001, 50)) = 1.0 // Настройте для планарного маппинга
        _PlanarAxisMode ("Planar Axis", Range(0, 2)) = 0 // 0: XY, 1: XZ, 2: YZ
        _TextureTransform ("Texture Transform (Scale U, V; Offset U, V)", Vector) = (1,1,0,0)
        [Header(Procedural Noise Perturbation)]
        _PerturbationStrength ("Perturbation Strength", Range(0, 0.5)) = 0.1 // Уменьшите, если шум слишком сильный
        _PerturbationScale ("Perturbation Scale", Range(0.01, 50)) = 1.0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
        LOD 100

        HLSLINCLUDE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half _OutputType;
                half4 _Color1;
                half4 _Color2;
                half4 _Color3;
                half _Threshold1;
                half _Threshold2;
                half _SharpBorders;
                half _MetricForRamp;
                half _VoronoiScale;
                half _VoronoiJitter;
                half _UVMappingMode;
                half _PlanarStrength;
                half _PlanarAxisMode;
                half4 _TextureTransform;
                half _PerturbationStrength;
                half _PerturbationScale;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS   : POSITION;
                half2 uv            : TEXCOORD0;
                half3 normalOS      : NORMAL;
                half2 lightmapUV    : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                half2 uv            : TEXCOORD0;
                half3 worldPos      : TEXCOORD1;
                half3 objectPos     : TEXCOORD2;
                half4 shadowCoord   : TEXCOORD3;
                half2 lightmapUV    : TEXCOORD4;
                float4 positionCS   : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            half2 hash_2D_to_2D(half2 p)
            {
                return frac(sin(half2(dot(p, half2(127.1, 311.7)), dot(p, half2(269.5, 183.3)))) * 43758.5453);
            }

            half2 SimpleValueNoise2D(half2 p)
            {
                half2 i = floor(p);
                half2 f = frac(p);
                half2 u = f * f * (3.0 - 2.0 * f);

                half2 h00 = hash_2D_to_2D(i);
                half2 h10 = hash_2D_to_2D(i + half2(1, 0));
                half2 h01 = hash_2D_to_2D(i + half2(0, 1));
                half2 h11 = hash_2D_to_2D(i + half2(1, 1));

                return lerp(lerp(h00, h10, u.x), lerp(h01, h11, u.x), u.y) * 0.5;
            }

            half2 voronoi(half2 x, half jitter, half metric_type, out half2 cell_id)
            {
                half2 n = floor(x);
                half2 f = frac(x);
                half min_dist1 = 8.0;
                half min_dist2 = 8.0;
                half2 closest_cell = half2(0, 0);

                for (int j = -1; j <= 1; j++)
                {
                    for (int i = -1; i <= 1; i++)
                    {
                        half2 g = half2(i, j);
                        half2 cell_id_temp = n + g;
                        half2 offset = hash_2D_to_2D(cell_id_temp);
                        half2 cellPoint = g + lerp(half2(0.5, 0.5), offset, jitter);
                        half2 r = cellPoint - f;

                        half dist;
                        if (metric_type < 0.5) dist = dot(r, r);
                        else if (metric_type < 1.5) dist = dot(r, r);
                        else dist = abs(r.x) + abs(r.y);

                        if (dist < min_dist1)
                        {
                            min_dist2 = min_dist1;
                            min_dist1 = dist;
                            closest_cell = g;
                        }
                        else if (dist < min_dist2)
                        {
                            min_dist2 = dist;
                        }
                    }
                }

                cell_id = n + closest_cell;
                if (metric_type < 1.5)
                {
                    min_dist1 = sqrt(min_dist1);
                    min_dist2 = sqrt(min_dist2);
                }
                return half2(min_dist1, min_dist2);
            }

            half4 getProceduralRampColor(half value, half4 c1, half4 c2, half4 c3, half t1, half t2, half sharp)
            {
                half actual_t1 = min(t1, t2);
                half actual_t2 = max(t1, t2);
                half factor1 = smoothstep(0, actual_t1 * (1 - sharp * 0.99), value);
                half factor2 = smoothstep(actual_t1 * (1 - sharp * 0.99), actual_t2 * (1 - sharp * 0.99), value);

                half4 color = lerp(c1, c2, factor1);
                color = lerp(color, c3, factor2);
                return color;
            }
        ENDHLSL

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }
            Cull Off

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.5
            #pragma vertex LitPassVert
            #pragma fragment LitPassFrag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_instancing

            Varyings LitPassVert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.worldPos = vertexInput.positionWS;
                output.objectPos = input.positionOS.xyz;
                output.uv = input.uv;
                OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.lightmapUV);

                #if defined(_MAIN_LIGHT_SHADOWS_CASCADE) || defined(_MAIN_LIGHT_SHADOWS)
                    output.shadowCoord = GetShadowCoord(vertexInput);
                #else
                    output.shadowCoord = half4(0,0,0,0);
                #endif

                return output;
            }

            half4 LitPassFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half2 coords = _UVMappingMode < 0.5 ? input.uv : (
                    _PlanarAxisMode < 0.5 ? input.objectPos.xy :
                    _PlanarAxisMode < 1.5 ? input.objectPos.xz : input.objectPos.yz
                );
                coords *= _TextureTransform.xy; // Масштабирование по U, V
                coords += _TextureTransform.zw; // Смещение по U, V
                coords /= _PlanarStrength;

                half2 offset = SimpleValueNoise2D(coords * _PerturbationScale) * _PerturbationStrength;
                coords += offset;
                coords *= _VoronoiScale;

                half2 cell_id;
                half2 distances = voronoi(coords, _VoronoiJitter, _MetricForRamp, cell_id);

                half4 color;
                if (_OutputType < 0.5)
                {
                    half value = saturate(distances.x);
                    color = getProceduralRampColor(value, _Color1, _Color2, _Color3, _Threshold1, _Threshold2, _SharpBorders);
                }
                else if (_OutputType < 1.5)
                {
                    half value = frac(sin(dot(cell_id, half2(127.1, 311.7))) * 43758.5453);
                    color = getProceduralRampColor(value, _Color1, _Color2, _Color3, _Threshold1, _Threshold2, _SharpBorders);
                    color.a = 1.0;
                }
                else
                {
                    half2 cell = floor(cell_id);
                    half checker = frac((cell.x + cell.y) * 0.5);
                    color = lerp(_Color1, _Color2, step(0.5, checker));
                    color.a = 1.0;
                }

                half shadow = 1.0;
                #if defined(_MAIN_LIGHT_SHADOWS_CASCADE) || defined(_MAIN_LIGHT_SHADOWS)
                    shadow = MainLightRealtimeShadow(input.shadowCoord);
                #endif
                color.rgb *= shadow;

                #ifdef LIGHTMAP_ON
                    half3 lightmap = SampleLightmap(input.lightmapUV, TransformObjectToWorldNormal(input.objectPos));
                    color.rgb *= lightmap;
                #endif

                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            Cull Off

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.5
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct AttributesShadow
            {
                float4 positionOS   : POSITION;
                half3 normalOS      : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct VaryingsShadow
            {
                float4 positionCS   : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            VaryingsShadow ShadowPassVertex(AttributesShadow input)
            {
                VaryingsShadow output = (VaryingsShadow)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                half3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                half3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                Light mainLight = GetMainLight();
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, mainLight.direction));

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif

                output.positionCS = positionCS;
                return output;
            }

            half4 ShadowPassFragment(VaryingsShadow input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return 0;
            }
            ENDHLSL
        }
    }
}