Shader "Custom/VoronoiAdvanced_ProceduralNoise_Stretch_URP_Gen"
{
    Properties
    {
        _OutputType ("Output Mode", Range(0, 2)) = 0
        _Color1 ("Color 1", Color) = (0.2,0.2,0.2,1)
        _Color2 ("Color 2", Color) = (0.5,0.5,0.5,1)
        _Color3 ("Color 3", Color) = (0.8,0.8,0.8,1)
        _Threshold1 ("Threshold 1 (0-1)", Range(0.01, 0.99)) = 0.33
        _Threshold2 ("Threshold 2 (0-1)", Range(0.01, 0.99)) = 0.66
        _SharpBorders ("Sharp Transitions (0-1)", Range(0, 1)) = 1
        [Header(Voronoi Parameters)]
        _VoronoiScale ("Voronoi Scale", Range(0.1, 100)) = 10.0
        _VoronoiJitter ("Voronoi Jitter", Range(0, 1)) = 0.9
        _MetricForRamp ("Metric for Ramp", Range(0, 2)) = 0.0
        _PerturbationStrength ("Perturbation Strength", Range(0, 0.5)) = 0.1
        _PerturbationScale ("Perturbation Scale", Range(0.01, 50)) = 1.0
        [Header(Mapping)]
        _UVMappingMode ("UV Mapping Mode", Range(0, 1)) = 0
        _PlanarStrength ("Planar Strength", Range(0.001, 50)) = 1.0
        _PlanarAxisMode ("Planar Axis", Range(0, 2)) = 0
        _TextureTransform ("Texture Transform (Scale U, V; Offset U, V)", Vector) = (1,1,0,0)
        _TextureWorldSize ("Texture World Size (Width, Height)", Vector) = (10,10,0,0)
        _TextureWorldOffset ("Texture World Offset (X, Y)", Vector) = (0,0,0,0)
        _VoronoiTexture ("Voronoi Texture", 2D) = "white" {}
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
                float4 _Color1;
                float4 _Color2;
                float4 _Color3;
                float _Threshold1;
                float _Threshold2;
                float _SharpBorders;
                float _VoronoiScale;
                float _VoronoiJitter;
                float _MetricForRamp;
                float _PerturbationStrength;
                float _PerturbationScale;
                float _UVMappingMode;
                float _PlanarStrength;
                float _PlanarAxisMode;
                float4 _TextureTransform;
                float2 _TextureWorldSize;
                float2 _TextureWorldOffset;
            CBUFFER_END

            TEXTURE2D(_VoronoiTexture);
            SAMPLER(sampler_VoronoiTexture);

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float3 normalOS     : NORMAL;
                float2 lightmapUV   : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv           : TEXCOORD0;
                float3 worldPos     : TEXCOORD1;
                float3 objectPos    : TEXCOORD2;
                float4 shadowCoord  : TEXCOORD3;
                float2 lightmapUV   : TEXCOORD4;
                float4 positionCS   : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float4 getProceduralRampColor(float value, float4 c1, float4 c2, float4 c3, float t1, float t2, float sharp)
            {
                float actual_t1 = min(t1, t2);
                float actual_t2 = max(t1, t2);
                float factor1 = smoothstep(0, actual_t1 * (1 - sharp * 0.99), value);
                float factor2 = smoothstep(actual_t1 * (1 - sharp * 0.99), actual_t2 * (1 - sharp * 0.99), value);

                float4 color = lerp(c1, c2, factor1);
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
                    output.shadowCoord = float4(0,0,0,0);
                #endif

                return output;
            }

            float4 LitPassFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Выбираем координаты (UV или планарные)
                float2 coords = _UVMappingMode < 0.5 ? input.uv : (
                    _PlanarAxisMode < 0.5 ? input.objectPos.xy :
                    _PlanarAxisMode < 1.5 ? input.objectPos.xz : input.objectPos.yz
                );

                // Применяем преобразования
                coords *= _TextureTransform.xy;
                coords += _TextureTransform.zw;
                coords /= _PlanarStrength;

                // Преобразуем координаты в пространство текстуры
                float2 texCoords = (coords - _TextureWorldOffset) / _TextureWorldSize;
                texCoords = texCoords - floor(texCoords); // Стабильная замена frac

                // Читаем данные Вороного из текстуры
                float4 voronoiData = SAMPLE_TEXTURE2D(_VoronoiTexture, sampler_VoronoiTexture, texCoords);
                float dist1 = voronoiData.r;
                float dist2 = voronoiData.g;
                float2 cell_id = voronoiData.ba;

                float4 color;
                if (_OutputType < 0.5)
                {
                    float value = saturate(dist1);
                    color = getProceduralRampColor(value, _Color1, _Color2, _Color3, _Threshold1, _Threshold2, _SharpBorders);
                }
                else if (_OutputType < 1.5)
                {
                    float value = frac(sin(dot(cell_id, float2(127.1, 311.7))) * 43758.5453);
                    color = getProceduralRampColor(value, _Color1, _Color2, _Color3, _Threshold1, _Threshold2, _SharpBorders);
                    color.a = 1.0;
                }
                else
                {
                    float2 cell = floor(cell_id);
                    float checker = frac((cell.x + cell.y) * 0.5);
                    color = lerp(_Color1, _Color2, step(0.5, checker));
                    color.a = 1.0;
                }

                float shadow = 1.0;
                #if defined(_MAIN_LIGHT_SHADOWS_CASCADE) || defined(_MAIN_LIGHT_SHADOWS)
                    shadow = MainLightRealtimeShadow(input.shadowCoord);
                #endif
                color.rgb *= shadow;

                #ifdef LIGHTMAP_ON
                    float3 lightmap = SampleLightmap(input.lightmapUV, TransformObjectToWorldNormal(input.objectPos));
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

            float4 ShadowPassFragment(VaryingsShadow input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return 0;
            }
            ENDHLSL
        }
    }
}