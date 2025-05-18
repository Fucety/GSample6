Shader "Custom/VoronoiAdvanced_ProceduralNoise_Stretch_URP"
{
    Properties
    {
        // Output Mode
        _OutputType ("Output Mode", Range(0, 2)) = 0 // 0: Color Ramp from Distance, 1: Cell Color (Ramp), 2: Checkerboard

        // Properties for all modes
        _Color1 ("Color 1", Color) = (0.2,0.2,0.2,1)
        _Color2 ("Color 2", Color) = (0.5,0.5,0.5,1)
        _Color3 ("Color 3", Color) = (0.8,0.8,0.8,1)
        _Threshold1 ("Threshold 1 (0-1)", Range(0.01, 0.99)) = 0.33
        _Threshold2 ("Threshold 2 (0-1)", Range(0.01, 0.99)) = 0.66
        _SharpBorders ("Sharp Transitions (0-1)", Range(0, 1)) = 1

        // Property for Color Ramp (if OutputType = 0)
        _MetricForRamp ("Metric for Color Ramp", Range(0, 2)) = 0 // 0: F1 Euclidean, 1: F2 Euclidean, 2: Manhattan F1

        // General Voronoi Parameters
        _VoronoiScale ("Voronoi Scale", Range(0.1, 100)) = 10.0
        _VoronoiJitter ("Voronoi Jitter", Range(0, 1)) = 0.9

        [Header(Mapping)]
        _UVMappingMode ("UV Mapping Mode", Range(0, 1)) = 0 // 0: UV, 1: Planar (XY/XZ/YZ)
        _PlanarStrength("Planar Strength (Overall Scale)", Range(0.001, 100)) = 1.0
        _PlanarAxisMode ("Planar Axis", Range(0, 2)) = 0 // 0: XY, 1: XZ, 2: YZ
        _StretchScale ("Stretch Scale (U, V)", Vector) = (1,1,0,0)

        [Header(Procedural Noise Perturbation)]
        _PerturbationStrength ("Perturbation Strength", Range(0, 1)) = 0.1
        _PerturbationScale ("Perturbation Scale", Range(0.01, 100)) = 1.0
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
                float _OutputType;
                half4 _Color1;
                half4 _Color2;
                half4 _Color3;
                float _Threshold1;
                float _Threshold2;
                float _SharpBorders;
                float _MetricForRamp;
                float _VoronoiScale;
                float _VoronoiJitter;
                float _UVMappingMode;
                float _PlanarStrength;
                float _PlanarAxisMode;
                float4 _StretchScale;
                float _PerturbationStrength;
                float _PerturbationScale;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float3 normalOS     : NORMAL;
                float2 lightmapUV   : TEXCOORD1; // UV для лайтмапов
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv           : TEXCOORD0;
                float3 worldPos     : TEXCOORD1;
                float3 objectPos    : TEXCOORD2;
                float4 shadowCoord  : TEXCOORD3;
                float2 lightmapUV   : TEXCOORD4; // UV для лайтмапов
                float4 positionCS   : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float2 hash_2D_to_2D(float2 p_in)
            {
                return float2(frac(sin(dot(p_in, float2(12.9898h, 78.233h))) * 43758.5453123h),
                              frac(sin(dot(p_in, float2(4.89845h, 7.23543h))) * 23421.6353245h));
            }

            float3 hash_2D_to_rgb_color(float2 p_in)
            {
                float r = frac(sin(dot(p_in, float2(12.9898h, 78.233h))) * 43758.5453h);
                float g = frac(sin(dot(p_in, float2(34.5678h, 45.9275h))) * 53758.5453h);
                float b = frac(sin(dot(p_in, float2(67.8912h, 23.4567h))) * 63758.5453h);
                return float3(r, g, b);
            }

            float2 SimpleValueNoise2D(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0h - 2.0h * f);

                float2 h00 = hash_2D_to_2D(i);
                float2 h10 = hash_2D_to_2D(i + float2(1.0h, 0.0h));
                float2 h01 = hash_2D_to_2D(i + float2(0.0h, 1.0h));
                float2 h11 = hash_2D_to_2D(i + float2(1.0h, 1.0h));

                float2 res = lerp(lerp(h00, h10, u.x), lerp(h01, h11, u.x), u.y);
                return res * 2.0h - 1.0h;
            }

            float2 voronoi(float2 x, float jitter_amount, float metric_type, out float2 out_euclidean_f1_cell_id)
            {
                float2 n = floor(x);
                float2 f = frac(x);

                float metric_min_dist1 = 100.0h;
                float metric_min_dist2 = 100.0h;
                float euclidean_f1_dist_sq_min = 100.0h;
                float2 mg_for_euclidean_f1 = float2(0.0h, 0.0h);

                for (int j = -2; j <= 2; j++)
                {
                    for (int i = -2; i <= 2; i++)
                    {
                        float2 g = float2(float(i), float(j));
                        float2 cell_being_checked_id = n + g;
                        float2 random_offset_in_cell = hash_2D_to_2D(cell_being_checked_id);
                        float2 point_in_cell = (1.0h - jitter_amount) * float2(0.5h, 0.5h) + jitter_amount * random_offset_in_cell;
                        float2 r = g + point_in_cell - f;
                        float d_euclidean_sq = dot(r, r);

                        if (d_euclidean_sq < euclidean_f1_dist_sq_min)
                        {
                            euclidean_f1_dist_sq_min = d_euclidean_sq;
                            mg_for_euclidean_f1 = g;
                        }

                        float d_current_metric;
                        if (metric_type < 1.5h) d_current_metric = d_euclidean_sq;
                        else d_current_metric = abs(r.x) + abs(r.y);

                        if (d_current_metric < metric_min_dist1)
                        {
                            metric_min_dist2 = metric_min_dist1;
                            metric_min_dist1 = d_current_metric;
                        }
                        else if (d_current_metric < metric_min_dist2)
                        {
                            metric_min_dist2 = d_current_metric;
                        }
                    }
                }
                out_euclidean_f1_cell_id = n + mg_for_euclidean_f1;
                if (metric_type < 1.5h)
                {
                    metric_min_dist1 = sqrt(metric_min_dist1);
                    metric_min_dist2 = sqrt(metric_min_dist2);
                }
                if (metric_type < 0.5h)      return float2(metric_min_dist1, metric_min_dist2);
                else if (metric_type < 1.5h) return float2(metric_min_dist2, metric_min_dist1);
                else                         return float2(metric_min_dist1, metric_min_dist2);
            }

            half4 getProceduralRampColor(float value, half4 c1, half4 c2, half4 c3, float t1, float t2, float use_sharp_borders)
            {
                half4 final_color;
                float actual_t1 = min(t1, t2);
                float actual_t2 = max(t1, t2);

                if (use_sharp_borders > 0.5h) // Sharp transitions
                {
                    if (value < actual_t1) final_color = c1;
                    else if (value < actual_t2) final_color = c2;
                    else final_color = c3;
                }
                else // Smooth transitions
                {
                    if (value < actual_t1)
                    {
                        float blend_factor = smoothstep(0.0h, actual_t1, value);
                        final_color = lerp(c1, c2, blend_factor);
                    }
                    else if (value < actual_t2)
                    {
                        float blend_factor = smoothstep(actual_t1, actual_t2, value);
                        final_color = lerp(c2, c3, blend_factor);
                    }
                    else
                    {
                        final_color = c3;
                    }
                }
                return final_color;
            }
        ENDHLSL

        Pass // Main Forward Lit Pass
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

                // Передаем UV лайтмапов
                OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.lightmapUV);

                #if defined(_MAIN_LIGHT_SHADOWS_CASCADE) || defined(_MAIN_LIGHT_SHADOWS)
                    output.shadowCoord = GetShadowCoord(vertexInput);
                #else
                    output.shadowCoord = float4(0,0,0,0);
                #endif

                return output;
            }

            half4 LitPassFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 coords_to_use;
                if (_UVMappingMode < 0.5h) coords_to_use = input.uv;
                else
                {
                    if (_PlanarAxisMode < 0.5h) coords_to_use = input.objectPos.xy;
                    else if (_PlanarAxisMode < 1.5h) coords_to_use = input.objectPos.xz;
                    else coords_to_use = input.objectPos.yz;
                }
                coords_to_use *= _StretchScale.xy;
                coords_to_use /= _PlanarStrength;

                float2 perturbation_offset = SimpleValueNoise2D(coords_to_use * _PerturbationScale) * _PerturbationStrength;
                coords_to_use += perturbation_offset;
                coords_to_use *= _VoronoiScale;

                float2 f1_cell_id_for_coloring;
                float2 ramp_distances = voronoi(coords_to_use, _VoronoiJitter, _MetricForRamp, f1_cell_id_for_coloring);

                half4 final_pixel_color;
                if (_OutputType < 0.5h) // Mode 0: Color Ramp from Distance
                {
                    float ramp_metric_value = ramp_distances.x;
                    float ramp_input_value = saturate(ramp_metric_value);
                    final_pixel_color = getProceduralRampColor(ramp_input_value, _Color1, _Color2, _Color3, _Threshold1, _Threshold2, _SharpBorders);
                }
                else if (_OutputType < 1.5h) // Mode 1: Cell Color (Ramp)
                {
                    float cell_random_value = frac(sin(dot(f1_cell_id_for_coloring, float2(12.9898h, 78.233h))) * 43758.5453h);
                    final_pixel_color = getProceduralRampColor(cell_random_value, _Color1, _Color2, _Color3, _Threshold1, _Threshold2, _SharpBorders);
                    final_pixel_color.a = 1.0h;
                }
                else // Mode 2: Checkerboard
                {
                    int2 cell_int_id = int2(floor(f1_cell_id_for_coloring));
                    int sum_coords = cell_int_id.x + cell_int_id.y;
                    final_pixel_color = (sum_coords % 2 == 0) ? _Color1 : _Color2;
                    final_pixel_color.a = 1.0h;
                }

                // Применение теней в реальном времени
                half shadowAttenuation = 1.0h;
                #if defined(_MAIN_LIGHT_SHADOWS_CASCADE) || defined(_MAIN_LIGHT_SHADOWS)
                    shadowAttenuation = MainLightRealtimeShadow(input.shadowCoord);
                #endif
                final_pixel_color.rgb *= shadowAttenuation;

                // Применение запеченных лайтмапов
                #ifdef LIGHTMAP_ON
                    half3 lightmap = SampleLightmap(input.lightmapUV, TransformObjectToWorldNormal(input.objectPos));
                    final_pixel_color.rgb *= lightmap;
                #endif

                return final_pixel_color;
            }
            ENDHLSL
        }

        Pass // ShadowCaster Pass
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
                float3 normalOS     : NORMAL;
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

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                // Получаем направление основного света
                Light mainLight = GetMainLight();
                float3 lightDirectionWS = mainLight.direction;

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

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