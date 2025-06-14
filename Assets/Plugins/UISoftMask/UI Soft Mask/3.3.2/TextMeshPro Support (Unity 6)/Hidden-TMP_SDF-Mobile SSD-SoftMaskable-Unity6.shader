﻿// Simplified SDF shader:
// - No Shading Option (bevel / bump / env map)
// - No Glow Option
// - Softness is applied on both side of the outline

Shader "Hidden/TextMeshPro/Mobile/Distance Field SSD (SoftMaskable)" {

Properties {
	_FaceColor		    ("Face Color", Color) = (1,1,1,1)
	_FaceDilate			("Face Dilate", Range(-1,1)) = 0

	_OutlineColor	    ("Outline Color", Color) = (0,0,0,1)
	_OutlineWidth		("Outline Thickness", Range(0,1)) = 0
	_OutlineSoftness	("Outline Softness", Range(0,1)) = 0

	_UnderlayColor		("Border Color", Color) = (0,0,0,.5)
	_UnderlayOffsetX 	("Border OffsetX", Range(-1,1)) = 0
	_UnderlayOffsetY 	("Border OffsetY", Range(-1,1)) = 0
	_UnderlayDilate		("Border Dilate", Range(-1,1)) = 0
	_UnderlaySoftness 	("Border Softness", Range(0,1)) = 0

	_WeightNormal		("Weight Normal", float) = 0
	_WeightBold			("Weight Bold", float) = .5

	_ShaderFlags		("Flags", float) = 0
	_ScaleRatioA		("Scale RatioA", float) = 1
	_ScaleRatioB		("Scale RatioB", float) = 1
	_ScaleRatioC		("Scale RatioC", float) = 1

	_MainTex			("Font Atlas", 2D) = "white" {}
	_TextureWidth		("Texture Width", float) = 512
	_TextureHeight		("Texture Height", float) = 512
	_GradientScale		("Gradient Scale", float) = 5
	_ScaleX				("Scale X", float) = 1
	_ScaleY				("Scale Y", float) = 1
	_PerspectiveFilter	("Perspective Correction", Range(0, 1)) = 0.875
	_Sharpness			("Sharpness", Range(-1,1)) = 0

	_VertexOffsetX		("Vertex OffsetX", float) = 0
	_VertexOffsetY		("Vertex OffsetY", float) = 0

	_ClipRect			("Clip Rect", vector) = (-32767, -32767, 32767, 32767)
	_MaskSoftnessX		("Mask SoftnessX", float) = 0
	_MaskSoftnessY		("Mask SoftnessY", float) = 0
	_MaskTex			("Mask Texture", 2D) = "white" {}
	_MaskInverse		("Inverse", float) = 0
	_MaskEdgeColor		("Edge Color", Color) = (1,1,1,1)
	_MaskEdgeSoftness	("Edge Softness", Range(0, 1)) = 0.01
	_MaskWipeControl	("Wipe Position", Range(0, 1)) = 0.5

	_StencilComp		("Stencil Comparison", Float) = 8
	_Stencil			("Stencil ID", Float) = 0
	_StencilOp			("Stencil Operation", Float) = 0
	_StencilWriteMask	("Stencil Write Mask", Float) = 255
	_StencilReadMask	("Stencil Read Mask", Float) = 255

    _CullMode           ("Cull Mode", Float) = 0
	_ColorMask			("Color Mask", Float) = 15
}

SubShader {
	Tags {
		"Queue"="Transparent"
		"IgnoreProjector"="True"
		"RenderType"="Transparent"
	}

	Stencil
	{
		Ref [_Stencil]
		Comp [_StencilComp]
		Pass [_StencilOp]
		ReadMask [_StencilReadMask]
		WriteMask [_StencilWriteMask]
	}

	Cull [_CullMode]
	ZWrite Off
	Lighting Off
	Fog { Mode Off }
	ZTest [unity_GUIZTestMode]
	Blend One OneMinusSrcAlpha
	ColorMask [_ColorMask]

	Pass {
		CGPROGRAM
		#pragma vertex VertShader
		#pragma fragment PixShader
		#pragma shader_feature __ OUTLINE_ON
		#pragma shader_feature __ UNDERLAY_ON UNDERLAY_INNER

		#pragma multi_compile __ UNITY_UI_CLIP_RECT
		#pragma multi_compile __ UNITY_UI_ALPHACLIP

        // ==== SOFTMASKABLE START ====
        #pragma shader_feature _ SOFTMASK_EDITOR
        #pragma shader_feature_local_fragment _ SOFTMASKABLE
        #if SOFTMASKABLE
        #include "Packages/com.coffee.softmask-for-ugui/Shaders/SoftMask.cginc"
        #endif
        // ==== SOFTMASKABLE END ====

		#include "UnityCG.cginc"
		#include "UnityUI.cginc"
		#include "Assets/Plugins/TextMesh Pro/Shaders/TMPro_Properties.cginc"

		struct vertex_t
		{
		    UNITY_VERTEX_INPUT_INSTANCE_ID
		    float4	position		: POSITION;
		    float3	normal			: NORMAL;
		    float4	color			: COLOR;
		    float4	texcoord0		: TEXCOORD0;
		    float2	texcoord1		: TEXCOORD1;
		};

		struct pixel_t
		{
		    UNITY_VERTEX_INPUT_INSTANCE_ID
		    UNITY_VERTEX_OUTPUT_STEREO
		    float4	position		: SV_POSITION;
		    float4	faceColor		: COLOR;
		    float4	outlineColor	: COLOR1;
		    float4	texcoord0		: TEXCOORD0;
		    float4	param			: TEXCOORD1;		// x = weight, y = no longer used
		    float2	mask			: TEXCOORD2;
		    #if (UNDERLAY_ON || UNDERLAY_INNER)
		    float4	texcoord2		: TEXCOORD3;
		    float4	underlayColor	: COLOR2;
		    #endif
			// ==== SOFTMASKABLE START ====
			#if SOFTMASK_EDITOR
			float4 worldPosition : TEXCOORD6;
			#endif
			// ==== SOFTMASKABLE END ====
		};
		
		float _UIMaskSoftnessX;
        float _UIMaskSoftnessY;

		float4 SRGBToLinear(float4 rgba)
		{
		    return float4(lerp(rgba.rgb / 12.92f, pow((rgba.rgb + 0.055f) / 1.055f, 2.4f), step(0.04045f, rgba.rgb)), rgba.a);
		}

		pixel_t VertShader(vertex_t input)
		{
		    pixel_t output;

		    UNITY_INITIALIZE_OUTPUT(pixel_t, output);
		    UNITY_SETUP_INSTANCE_ID(input);
		    UNITY_TRANSFER_INSTANCE_ID(input, output);
		    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

		    float bold = step(input.texcoord0.w, 0);

		    float4 vert = input.position;
		    vert.x += _VertexOffsetX;
		    vert.y += _VertexOffsetY;

		    float4 vPosition = UnityObjectToClipPos(vert);

		    float weight = lerp(_WeightNormal, _WeightBold, bold) / 4.0;
		    weight = (weight + _FaceDilate) * _ScaleRatioA * 0.5;

		    // Generate UV for the Masking Texture
		    float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
		    float2 maskUV = (vert.xy - clampedRect.xy) / (clampedRect.zw - clampedRect.xy);

		    float4 color = input.color;
		    #if (FORCE_LINEAR && !UNITY_COLORSPACE_GAMMA)
		    color = SRGBToLinear(input.color);
		    #endif

		    float opacity = color.a;
		    #if (UNDERLAY_ON | UNDERLAY_INNER)
		    opacity = 1.0;
		    #endif

		    float4 faceColor = float4(color.rgb, opacity) * _FaceColor;
		    faceColor.rgb *= faceColor.a;

		    float4 outlineColor = _OutlineColor;
		    outlineColor.a *= opacity;
		    outlineColor.rgb *= outlineColor.a;

		    output.position = vPosition;
		    output.faceColor = faceColor;
		    output.outlineColor = outlineColor;
		    output.texcoord0 = float4(input.texcoord0.xy, maskUV.xy);
		    output.param = float4(0.5 - weight, 0, _OutlineWidth * _ScaleRatioA * 0.5, 0);

		    float2 mask = float2(0, 0);
		    #if UNITY_UI_CLIP_RECT
		    mask = vert.xy * 2 - clampedRect.xy - clampedRect.zw;
		    #endif
		    output.mask = mask;

		    #if (UNDERLAY_ON || UNDERLAY_INNER)
		    float4 underlayColor = _UnderlayColor;
		    underlayColor.rgb *= underlayColor.a;

		    float x = -(_UnderlayOffsetX * _ScaleRatioC) * _GradientScale / _TextureWidth;
		    float y = -(_UnderlayOffsetY * _ScaleRatioC) * _GradientScale / _TextureHeight;

		    output.texcoord2 = float4(input.texcoord0 + float2(x, y), input.color.a, 0);
		    output.underlayColor = underlayColor;
		    #endif

			// ==== SOFTMASKABLE START ====
			#if SOFTMASK_EDITOR
			output.worldPosition = input.position;
			#endif
			// ==== SOFTMASKABLE END ====

		    return output;
		}

		float4 PixShader(pixel_t input) : SV_Target
		{
		    UNITY_SETUP_INSTANCE_ID(input);

		    float d = tex2D(_MainTex, input.texcoord0.xy).a;

		    float pixelSize = abs(ddx(input.texcoord0.y)) + abs(ddy(input.texcoord0.y));
		    pixelSize *= _TextureHeight * 0.75;
		    float scale = 1 / pixelSize * _GradientScale * (_Sharpness + 1);

		    #if (UNDERLAY_ON | UNDERLAY_INNER)
		    float layerScale = scale;
		    layerScale /= 1 + ((_UnderlaySoftness * _ScaleRatioC) * layerScale);
		    float layerBias = input.param.x * layerScale - .5 - ((_UnderlayDilate * _ScaleRatioC) * .5 * layerScale);
		    #endif

		    scale /= 1 + (_OutlineSoftness * _ScaleRatioA * scale);

		    float4 faceColor = input.faceColor * saturate((d - input.param.x) * scale + 0.5);

		    #if OUTLINE_ON
		    float4 outlineColor = lerp(input.faceColor, input.outlineColor, sqrt(min(1.0, input.param.z * scale * 2)));
		    faceColor = lerp(outlineColor, input.faceColor, saturate((d - input.param.x - input.param.z) * scale + 0.5));
		    faceColor *= saturate((d - input.param.x + input.param.z) * scale + 0.5);
		    #endif

		    #if UNDERLAY_ON
		    d = tex2D(_MainTex, input.texcoord2.xy).a * layerScale;
		    faceColor += float4(_UnderlayColor.rgb * _UnderlayColor.a, _UnderlayColor.a) * saturate(d - layerBias) * (1 - faceColor.a);
		    #endif

		    #if UNDERLAY_INNER
		    float bias = input.param.x * scale - 0.5;
		    float sd = saturate(d * scale - bias - input.param.z);
		    d = tex2D(_MainTex, input.texcoord2.xy).a * layerScale;
		    faceColor += float4(_UnderlayColor.rgb * _UnderlayColor.a, _UnderlayColor.a) * (1 - saturate(d - layerBias)) * sd * (1 - faceColor.a);
		    #endif

		    #if MASKING
		    float a = abs(_MaskInverse - tex2D(_MaskTex, input.texcoord0.zw).a);
		    float t = a + (1 - _MaskWipeControl) * _MaskEdgeSoftness - _MaskWipeControl;
		    a = saturate(t / _MaskEdgeSoftness);
		    faceColor.rgb = lerp(_MaskEdgeColor.rgb * faceColor.a, faceColor.rgb, a);
		    faceColor *= a;
		    #endif

		    // Alternative implementation to UnityGet2DClipping with support for softness
		    #if UNITY_UI_CLIP_RECT
		    half2 maskSoftness = half2(max(_UIMaskSoftnessX, _MaskSoftnessX), max(_UIMaskSoftnessY, _MaskSoftnessY));
		    float2 maskZW = 0.25 / (0.25 * maskSoftness + 1 / scale);
		    float2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(input.mask.xy)) * maskZW);
		    faceColor *= m.x * m.y;
		    #endif

		    #if (UNDERLAY_ON | UNDERLAY_INNER)
		    faceColor *= input.texcoord2.z;
		    #endif

            // ==== SOFTMASKABLE START ====
            #if SOFTMASKABLE
            faceColor *= SoftMask(input.position, input.worldPosition, faceColor.a);
            #endif
            // ==== SOFTMASKABLE END ====

		    #if UNITY_UI_ALPHACLIP
		    clip(faceColor.a - 0.001);
		    #endif

		    return faceColor;
		}
		ENDCG
	}
}

CustomEditor "TMPro.EditorUtilities.TMP_SDFShaderGUI"
}
