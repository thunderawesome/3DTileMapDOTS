Shader "TriplanarTutorial/Triplanar_Final" 
{
	Properties 
	{
		[MainTexture] _BaseMap("Texture", 2D) = "white" {}
		_TextureScale ("Texture Scale", float) = 1
		_TriplanarBlendSharpness ("Blend Sharpness", float) = 1
	}
	SubShader 
	{
		Tags 
		{ 
			"RenderPipeline"="UniversalPipeline"
			"RenderType"="Opaque"
			"LightMode" = "UniversalForward"
		}

		HLSLINCLUDE
		// Including the following two function is enought for shading with Universal Pipeline. Everything is included in them.
		// Core.hlsl will include SRP shader library, all constant buffers not related to materials (perobject, percamera, perframe).
		// It also includes matrix/space conversion functions and fog.
		// Lighting.hlsl will include the light functions/data to abstract light constants. You should use GetMainLight and GetLight functions
		// that initialize Light struct. Lighting.hlsl also include GI, Light BDRF functions. It also includes Shadows.

		// Required by all Universal Render Pipeline shaders.
		// It will include Unity built-in shader variables (except the lighting variables)
		// (https://docs.unity3d.com/Manual/SL-UnityShaderVariables.html
		// It will also include many utilitary functions. 
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

		CBUFFER_START(UnityPerMaterial)

		float1 _TextureScale;
		float1 _TriplanarBlendSharpness;

		CBUFFER_END

		ENDHLSL

		Pass
		{ 
			HLSLPROGRAM

			#pragma exclude_renderers gles gles3 glcore
            #pragma target 4.5

			#pragma vertex vert
			#pragma fragment frag

			// Include this if you are doing a lit shader. This includes lighting shader variables,
			// lighting and shadow functions
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			// Material shader variables are not defined in SRP or LWRP shader library.
            // This means _BaseColor, _BaseMap, _BaseMap_ST, and all variables in the Properties section of a shader
            // must be defined by the shader itself. If you define all those properties in CBUFFER named
            // UnityPerMaterial, SRP can cache the material properties between frames and reduce significantly the cost
            // of each drawcall.
            // In this case, for sinmplicity LitInput.hlsl is included. This contains the CBUFFER for the material
            // properties defined above. As one can see this is not part of the ShaderLibrary, it specific to the
            // LWRP Lit shader.
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"

			// Main Light Shadows
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
 
			// Additional Lights & Shadows
			#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
 
			// Soft Shadows
			#pragma multi_compile _ _SHADOWS_SOFT
 
			// Other (Mixed lighting, baked lightmaps, fog)
			#pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
			#pragma multi_compile _ DIRLIGHTMAP_COMBINED
			#pragma multi_compile _ LIGHTMAP_ON

			// -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fog      

			//--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON	

			struct Attributes
			{
				float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
				float4 tangent : TANGENT;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			}; 

			struct Varyings{
				float4 positionCS : SV_POSITION;  
				float2 uv: TEXCOORD0;
				float3 normal: TEXCOORD1;
				float4 positionWSAndFogFactor : TEXCOORD2; // xyz: positionWS, w: vertex fog factor

				UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
			};            

			Varyings vert (Attributes v)
			{
				Varyings o;

				UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				// VertexPositionInputs contains position in multiple spaces (world, view, homogeneous clip space)
                // Our compiler will strip all unused references (say you don't use view space).
                // Therefore there is more flexibility at no additional cost with this struct.
                VertexPositionInputs vertexInput = GetVertexPositionInputs(v.positionOS.xyz);

				// Similar to VertexPositionInputs, VertexNormalInputs will contain normal, tangent and bitangent
                // in world space. If not used it will be stripped.
                VertexNormalInputs vertexNormalInput = GetVertexNormalInputs(v.normal, v.tangent);

				// Computes fog factor per-vertex.
                float fogFactor = ComputeFogFactor(vertexInput.positionCS.z);				

				o.positionWSAndFogFactor = float4(vertexInput.positionWS, fogFactor);

				o.uv = TRANSFORM_TEX(v.uv, _BaseMap);

				o.normal = vertexNormalInput.normalWS;
				o.positionCS = vertexInput.positionCS;

				return o;
			}

			float4 frag (Varyings i) : SV_Target
			{
				float3 normal = normalize(i.normal);
				// Find our UVs for each axis based on world position of the fragment.
				float2 yUV = normal.xz / _TextureScale;
				float2 xUV = normal.zy / _TextureScale;
				float2 zUV = normal.xy / _TextureScale;
				// Now do texture samples from our diffuse map with each of the 3 UV set's we've just made.
				float3 yDiff = SAMPLE_TEXTURE2D (_BaseMap, sampler_BaseMap, yUV).xyz;
				float3 xDiff = SAMPLE_TEXTURE2D (_BaseMap, sampler_BaseMap, xUV).xyz;
				float3 zDiff = SAMPLE_TEXTURE2D (_BaseMap, sampler_BaseMap, zUV).xyz;
				// Get the absolute value of the world normal.
				// Put the blend weights to the power of BlendSharpness, the higher the value, 
				// the sharper the transition between the planar maps will be.
				float3 blendWeights = pow (abs(normal), _TriplanarBlendSharpness);
				// Divide our blend mask by the sum of it's components, this will make x+y+z=1
				blendWeights = blendWeights / (blendWeights.x + blendWeights.y + blendWeights.z);
				// Finally, blend together all three samples based on the blend mask.
				float3 result = xDiff * blendWeights.x + yDiff * blendWeights.y + zDiff * blendWeights.z;
				return float4(result, 1);//float4 (i.normal.xyz,1);
			}
			ENDHLSL
		}
	}
	FallBack "Universal Render Pipeline/Simple Lit"
}