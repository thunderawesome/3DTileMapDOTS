Shader "Simple/Waterfall" {
	Properties {
		[Space]
		[Header(Water)]
		_TColor ("Top Water Tint", Color) = (0,1,1,1)
		_WaterColor ("Side Water Tint", Color) = (0,0.6,1,1)
		_BrightNess ("Water Brightness", Range(0.5,2)) = 1.2
		[Space]
		[Header(Surface Noise and Movement)]
		_SideNoiseTex ("Side Water Texture", 2D) = "white" {}
		_TopNoiseTex ("Top Water Texture", 2D) = "white" {}
		_HorSpeed ("Horizontal Flow Speed", Range(-4,4)) = 0.14
		_VertSpeed("Vertical Flow Speed", Range(0,10)) = 6.8
		_TopScale ("Top Noise Scale", Range(0,1)) = 0.4
		_NoiseScale ("Side Noise Scale", Range(0,1)) = 0.04
		 [Toggle(VERTEX)] _VERTEX("Use Vertex Colors", Float) = 0
		
		[Space]
		[Header(Foam)]
		_FoamColor ("Foam Tint", Color) = (1,1,1,1)
		_Foam ("Edgefoam Width", Range(1,10)) = 2.35
		_TopSpread("Foam Position", Range(0,6)) = 0.05
		_Softness ("Foam Softness", Range(0,0.5)) = 0.1
		_EdgeWidth("Foam Width", Range(0,2)) = 0.4

		[Space]
		[Header(Rim Light)]
		 [Toggle(RIM)] _RIM("Hard Rim", Float) = 0
		_RimPower("Rim Power", Range(1,20)) = 18
		_RimColor("Rim Color", Color) = (0,0.5,0.25,1)
		
	}
	SubShader {
		 Tags
		 { 
			 "Queue" = "Transparent"
			 "RenderPipeline"="UniversalPipeline"
		 }
		LOD 200
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			HLSLPROGRAM		

			#pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x

			#pragma vertex vert
			#pragma fragment frag

			// make fog work
            #pragma multi_compile_fog	

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

			// Tranforms position from object to camera space
			inline float3 UnityObjectToViewPos( in float3 pos )
			{
				return mul(UNITY_MATRIX_V, mul(unity_ObjectToWorld, float4(pos, 1.0))).xyz;
			}
			inline float3 UnityObjectToViewPos(float4 pos) // overload for float4; avoids "implicit truncation" warning for existing shaders
			{
				return UnityObjectToViewPos(pos.xyz);
			}

			#define COMPUTE_EYEDEPTH(o) o = -UnityObjectToViewPos( v.vertex ).z

			TEXTURE2D(_SideNoiseTex);
			SAMPLER(sampler_SideNoiseTex);

			TEXTURE2D(_TopNoiseTex);
			SAMPLER(sampler_TopNoiseTex);

			TEXTURE2D(_CameraDepthTexture);
			SAMPLER(sampler_CameraDepthTexture);

			CBUFFER_START(UnityPerMaterial)

			float4 _SideNoiseTex_ST;
			float4 _TopNoiseTex_ST;
			float4 _CameraDepthTexture_ST;

			float4 _FoamColor, _WaterColor, _RimColor,  _TColor;
			float _HorSpeed, _TopScale, _TopSpread, _EdgeWidth, _RimPower,_NoiseScale , _VertSpeed;
			float _BrightNess, _Foam, _Softness;

			CBUFFER_END

			struct MeshData
			{
				float4 vertex : POSITION;
				float2 uv0 : TEXCOORD0;
				float2 uv1 : TEXCOORD1;
				float3 normal : NORMAL;		
				float4 tangent : TANGENT;
				float4 color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv0 : TEXCOORD0;
				float2 uv1 : TEXCOORD1;
				float3 normal: TEXCOORD2;
				float3 worldPos : TEXCOORD3;	
				float3 viewDir : TEXCOORD4;
				float4 screenPos : TEXCOORD5; // screen position for edgefoam		
				float eyeDepth: TEXCOORD6;
				float4 color : COLOR; // vertex colors
				//UNITY_FOG_COORDS(1)
				UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
			};

			v2f vert (MeshData v)
			{
				v2f o;

				UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);					

				// VertexPositionInputs contains position in multiple spaces (world, view, homogeneous clip space)
                // Our compiler will strip all unused references (say you don't use view space).
                // Therefore there is more flexibility at no additional cost with this struct.
                VertexPositionInputs vertexInput = GetVertexPositionInputs(v.vertex.xyz);

				// Similar to VertexPositionInputs, VertexNormalInputs will contain normal, tangent and bitangent
                // in world space. If not used it will be stripped.
                VertexNormalInputs vertexNormalInput = GetVertexNormalInputs(v.normal, v.tangent);

				o.normal = vertexNormalInput.normalWS;
				o.vertex = vertexInput.positionCS;
				o.worldPos = vertexInput.positionCS.xyz;
				o.viewDir = GetWorldSpaceViewDir(v.vertex.xyz);
				o.screenPos = vertexInput.positionNDC;
				o.color = float4(v.color.rgb, v.color.a);

				o.uv0 = TRANSFORM_TEX(v.uv0, _TopNoiseTex);
				o.uv1 = TRANSFORM_TEX(v.uv1, _SideNoiseTex);

				//COMPUTE_EYEDEPTH(o.eyeDepth); // depth for edgefoam
				//UNITY_TRANSFER_FOG(o,o.vertex);				
				return o;
			}			

			float4 frag(v2f i) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

	//			// get the world normal
	//			float3 worldNormal = normalize(i.normal);
	//			// grab the vertex colors from the model
	//			float3 vertexColors = i.color.rgb;
	//			// normal for triplanar mapping
	//			float3 blendNormal = saturate(pow(worldNormal * 1.4,4));
			
		
	//	#if VERTEX // use vertex colors for flow
	//			float3 flowDir= (vertexColors * 2.0f) - 1.0f;
	//	#else // or world normal
	//			float3 flowDir= -(worldNormal * 2.0f) - 1.0f;
	//	#endif
	//		// horizontal flow speed
	//		flowDir *= _HorSpeed;

	//		// flowmap blend timings
	//		float timing = frac(_Time.y * 0.5f + 0.5f);
	//		float timing2 = frac(_Time.y* 0.5f);
	//		float timingLerp = abs((0.5f - timing) / 0.5f);

	//		// move 2 textures at slight different speeds fased on the flowdirection
	//		float3 topTex1 = SAMPLE_TEXTURE2D(_TopNoiseTex, sampler_TopNoiseTex, i.uv0 * i.worldPos.xz * _TopScale + (flowDir.xz * timing)).xyz;
	//		float3 topTex2 = SAMPLE_TEXTURE2D(_TopNoiseTex, sampler_TopNoiseTex, i.uv0 * i.worldPos.xz * _TopScale + (flowDir.xz * timing2)).xyz;
	
	//		// vertical flow speed
	//		float vertFlow = _Time.y * _VertSpeed;

	//		// noise sides
	//		float3 TopFoamNoise = lerp(topTex1, topTex2, timingLerp);
	//		float3 SideFoamNoiseZ = SAMPLE_TEXTURE2D(_SideNoiseTex, sampler_SideNoiseTex, float2(i.worldPos.z* 10, i.worldPos.y + vertFlow) * _NoiseScale).xyz;
	//		float3 SideFoamNoiseX = SAMPLE_TEXTURE2D(_SideNoiseTex, sampler_SideNoiseTex, float2(i.worldPos.x* 10, i.worldPos.y + vertFlow) * _NoiseScale).xyz;

	//		// lerped together all sides for noise texture
	//		float3 noisetexture = SideFoamNoiseX;
	//		noisetexture = lerp(noisetexture, SideFoamNoiseZ, blendNormal.x);
	//		noisetexture = lerp(noisetexture, TopFoamNoise, blendNormal.y);

	//		// add noise to normal
	//		i.normal *= noisetexture;

	//		// edge foam calculation
	//		half depth = LinearEyeDepth(_CameraDepthTexture.Sample (sampler_CameraDepthTexture, i.screenPos.xy / i.screenPos.w).r, _ZBufferParams);
	//		half4 foamLine =1 - saturate(_Foam * float4(noisetexture,1) * (depth - i.screenPos.w));// foam line by comparing depth and screenposition
		
	//		// rimline
	//#if RIM
	//		int rim = 1.0 - saturate(dot(normalize(i.viewDir) , i.normal));
	//#else
	//		half rim = 1.0 - saturate(dot(normalize(i.viewDir) , i.normal));
	//#endif
	//		float3 colorRim = _RimColor.rgb * pow (rim, _RimPower);
		
	//		// Normalbased Foam
	//		float worldNormalDotNoise = dot(i.normal , worldNormal.y);	
	//		float3 foam = (smoothstep(_TopSpread, _TopSpread + _Softness, worldNormalDotNoise) * smoothstep(worldNormalDotNoise,worldNormalDotNoise + _Softness, _TopSpread + _EdgeWidth));

	//		// combine depth foam and foam + add color
	//		float3 combinedFoam =  (foam + foamLine.rgb) * _FoamColor.xyz;
		
	//		// colors lerped over blendnormal
	//		float4 color = lerp(_WaterColor, _TColor, blendNormal.y) * _BrightNess;
	//		float4 albedo = color;

	//		// glowing combined foam and colored rim
	//		float3 emission = combinedFoam + colorRim ;
		
	//		// clamped alpha
	//		float alpha = clamp(color.a + combinedFoam + foamLine.a, 0, 1).x;
		
			//return float4(albedo.xyz * emission.xyz, 1);
			return float4(1,1,1,1);

			}
			ENDHLSL
		}
	}
	FallBack "Universal Render Pipeline/Simple Lit"
}