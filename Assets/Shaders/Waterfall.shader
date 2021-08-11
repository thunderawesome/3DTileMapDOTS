Shader "FX/Waterfall Interactive Adv" 
{
	Properties
	{
		_Smoothness("Smoothness", Range(0,1)) = 0.6
		_Metallic("Metallic", Range(0,1)) = 0.6
		[Space]
		[Header(Water)]
		_TColor("Deep Tint", Color) = (0,1,1,1)
		_WaterColor("Edge Tint", Color) = (0,0.6,1,1)
		_DepthOffset("Depth Offset", Range(-10,10)) = 0
		_Stretch("Depth Stretch", Range(0,5)) = 2
		_Brightness("Water Brightness", Range(0.5,2)) = 1.2
		_Distort("Distortion", Range(0,1)) = 0.6

		[Space]
		[Header(Surface Noise and Movement)]
		_SideNoiseTex("Side Water Texture", 2D) = "white" {}
		_TopNoiseTex("Top Water Texture", 2D) = "white" {}
		_HorSpeed("Horizontal Flow Speed", Range(0,4)) = 0.14
		_VertSpeed("Vertical Flow Speed", Range(0,60)) = 6.8
		_TopScale("Top Noise Scale", Range(0,1)) = 0.4
		_NoiseScale("Side Noise Scale", Range(0,1)) = 0.04
		[Toggle(VERTEX)] _VERTEX("Use Vertex Colors", Float) = 0

		[Space]
		[Header(Foam)]
		_FoamColor("Foam Tint", Color) = (1,1,1,1)
		_Foam("Edgefoam Width", Range(1,50)) = 2.35
		_TopSpread("Foam Position", Range(-1,6)) = 0.05
		_Softness("Foam Softness", Range(0,0.5)) = 0.1
		_EdgeWidth("Foam Width", Range(0,2)) = 0.4

		[Space]
		[Header(Rim Light)]
		_RimPower("Rim Power", Range(1,20)) = 18
		_RimColor("Rim Color", Color) = (0,0.5,0.25,1)

		[Space]
		[Header(Vertex Movement)]
		_Amount("Wave Amount", Range(0,10)) = 0.6
		_SpeedV("Speed", Range(0,10)) = 0.5
		_Height("Wave Height", Range(0,1)) = 0.1

		 [Space]
		[Header(Reflections)]
		_ReflectionTex("Refl Texture", 2D) = "black" {}
		_Reflectivity("Reflectivity", Range(0,1)) = 0.6
		

		[Space]
		[Header(Water Sparkle)]
		_FresnelScale("Sparkle Distort", Range(0,5)) = 0.6
		_FresnelBias("Sparkle Bias", Range(0,5)) = 0.6
		_FresnelPower("Sparkle Power", Range(-5,5)) = 0.6		
	}
	SubShader
	{
		Tags
		{ 
			"RenderPipeline"="UniversalPipeline"
			"Queue" = "Transparent"
			"RenderType"="Transparent"
		}
		LOD 200
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			// Upgrade NOTE: excluded shader from DX11; has structs without semantics (struct v2f members screenPos,eyeDepth,viewInterpolator)
			#pragma exclude_renderers d3d11

			#pragma vertex vert
			#pragma fragment frag

			 // Use shader model 3.0 target, to get nicer looking lighting
			 #pragma target 3.5 
	
			 sampler2D _SideNoiseTex, _TopNoiseTex;
			 uniform sampler2D _CameraDepthTexture; //Depth Texture			 
			 sampler2D _CameraOpaqueTexture;
			 float4 _CameraOpaqueTexture_ST;

			 struct MeshData
			 {
				  float4 vertex : POSITION;
				  float2 uv : TEXCOORD0;
				  float3 normal : NORMAL;
			 }

			 struct v2f {
				 float4 vertex : SV_POSITION;
				 float3 normal: NORMAL;
				 float2 uv : TEXCOORD0;				 
				 float3 viewDir : TEXCOORD1;// view direction for rim
				 float3 wPos : TEXCOORD2;	
				
				 
				 float4 color : COLOR; // vertex colors
				 float4 screenPos: TEXCOORD3; // screen position for edgefoam
				 float eyeDepth : TEXCOORD4;// depth for edgefoam
				 float4 viewInterpolator : TEXCOORD5;				
			 };

			 inline half Fresnel(half3 viewVector, half3 normal, half bias, half power)
			 {
				 half facing = clamp(1.0 - max(dot(-viewVector, normal), 0.0), 0.0, 1.0);
				 half refl2Refr = saturate(bias + (1.0 - bias) * pow(facing, power));
				 return refl2Refr;
			 }

			 float _SpeedV, _Amount, _Height;
			 fixed4 _FoamColor, _WaterColor, _RimColor, _TColor;
			 fixed _HorSpeed, _TopScale, _TopSpread, _EdgeWidth, _RimPower, _NoiseScale, _VertSpeed;
			 float _Brightness, _Foam, _Softness;
			 float _DepthOffset, _Stretch;
			 sampler2D _ReflectionTex;
			 float _Reflectivity, _Distort, _FresnelScale, _FresnelBias, _FresnelPower;
			 float _Smoothness, _Metallic;

			 v2f vert(MeshData v)
			 {
				 v2f o;
				 UNITY_INITIALIZE_OUTPUT(v2f, o);
				 COMPUTE_EYEDEPTH(o.eyeDepth);
				 float3 normal = mul(unity_ObjectToWorld, v.normal);
				 float3 wPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				 half3 tex = tex2Dlod(_SideNoiseTex, float4(wPos.xz * _TopScale * 1, 1,1));
				 float3 movement = sin(_Time.z * _SpeedV + (v.vertex.x * v.vertex.z * _Amount * tex)) * _Height * (1 - normal.y);

				 v.vertex.xyz += movement;

				 half3 worldSpaceVertex = mul(unity_ObjectToWorld, (v.vertex)).xyz;
				 o.viewInterpolator.xyz = worldSpaceVertex - _WorldSpaceCameraPos;

			 }

			 uniform float3 _Position;
			 uniform sampler2D _GlobalEffectRT;
			 uniform float _OrthographicCamSize;

			 float4 frag(v2f i) : SV_Target
			 {
				// get the world normal
				float3 normal = normalize(i.normal);
				// grab the vertex colors from the model
				float3 vertexColors = i.color.rgb;
				// normal for triplanar mapping
				float3 blendNormal = saturate(pow(normal * 1.4,4));


	   #if VERTEX // use vertex colors for flow
				float3 flowDir = (vertexColors * 2.0f) - 1.0f;
	   #else // or world normal
				float3 flowDir = -(normal * 2.0f) - 1.0f;
	   #endif
			   // horizontal flow speed
				flowDir *= _HorSpeed;

			   // flowmap blend timings
			    float timing = frac(_Time[1] * 0.5 + 0.5);
			    float timing2 = frac(_Time[1] * 0.5);
			    float timingLerp = abs((0.5 - timing) / 0.5);

			    // move 2 textures at slight different speeds fased on the flowdirection
			    half3 topTex1 = tex2D(_TopNoiseTex, (i.wPos.xz * _TopScale) + flowDir.xz * timing);
			    half3 topTex2 = tex2D(_TopNoiseTex, (i.wPos.xz * _TopScale) + flowDir.xz * timing2);

			    // vertical flow speed
			    float vertFlow = _Time.y * _VertSpeed;

			    // rendertexture UV
			    float2 uv = i.wPos.xz - _Position.xz;
			    uv = uv / (_OrthographicCamSize * 2);
			    uv += 0.5;

			    // Ripples
			    //float ripples = tex2D(_GlobalEffectRT, uv).b;

			    // noise sides
			    float3 TopFoamNoise = lerp(topTex1, topTex2, timingLerp);// + ripples;

			    float3 SideFoamNoiseZ = tex2D(_SideNoiseTex, float2(i.wPos.z * 10, i.wPos.y + vertFlow) * _NoiseScale);
			    float3 SideFoamNoiseX = tex2D(_SideNoiseTex, float2(i.wPos.x * 10, i.wPos.y + vertFlow)  * _NoiseScale);

			    float3 SideFoamNoiseZE = tex2D(_TopNoiseTex, float2(i.wPos.z * 10, i.wPos.y + vertFlow) * _NoiseScale * 0.4);
			    float3 SideFoamNoiseXE = tex2D(_TopNoiseTex, float2(i.wPos.x * 10, i.wPos.y + vertFlow)  * _NoiseScale * 0.4);

			    // lerped together all sides for noise texture
			    float3 noisetexture = (SideFoamNoiseX + SideFoamNoiseXE) * 0.5;
			    noisetexture = lerp(noisetexture, (SideFoamNoiseZ + SideFoamNoiseZE) * 0.5, blendNormal.x);
			    noisetexture = lerp(noisetexture, TopFoamNoise, blendNormal.y);

			    // Normalbased Foam
			    float normalDotNoise = dot(i.normal, normal.y + 0.3);
			    normalDotNoise *= noisetexture;

			    // add noise to normal
			    i.normal = normal + noisetexture;
			    i.normal = i.normal;// + ripples;

			    // noise distort
			    float noiseDist = (normalDotNoise * _Distort);

			    // edge foam calculation
			    half depth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture ,float4(i.screenPos.x,i.screenPos.y + noiseDist, i.screenPos.z, i.screenPos.w))); // depth
			    float foamLineS = 1 - saturate(_Foam * float4(noisetexture,1) * (depth - i.screenPos.w));// foam line by comparing depth and screenposition
			    float foamLine = smoothstep(0.6, 0.8, foamLineS) * 3;
			  
			    // water shine sparkles
				half3 viewVector = normalize(i.viewInterpolator.xyz);
			    float3 norm = normalDotNoise;
			    norm.xz *= _FresnelScale;
			    half sparkle = Fresnel(viewVector, norm, _FresnelBias, _FresnelPower);
				sparkle = step(0.8, sparkle) *step(sparkle, 0.87);
				sparkle *= smoothstep(0.5, 0.55, normalDotNoise);

				// colored rim
			    float rim = 1 - saturate(dot(normalize(i.viewDir) , smoothstep(0.2, 0.3, noisetexture)));
			    float3 colorRim = _RimColor.rgb * pow(rim, _RimPower);

			   // side foam
			    float3 foamS = (smoothstep(_TopSpread, _TopSpread + _Softness, normalDotNoise) * smoothstep(normalDotNoise, normalDotNoise + _Softness, _TopSpread + _EdgeWidth));

				// no foam on top of water
			    foamS *= saturate(1 - normal.y);
			    foamS *= 3;
			  
				// sharper ripples for foam
			   // ripples = smoothstep(0.5, 0.6, ripples);
			    // combine depth foam and foam + add color
			    float3 combinedFoam = (foamS + foamLine) * _FoamColor;
	 
				//grabpass for refraction
			    //half4 bgcolor = tex2Dproj(_CameraOpaqueTexture, float4(i.screenPos.x ,i.screenPos.y + noiseDist, i.screenPos.z, i.screenPos.w));

			    // colors lerped over depth
			    float4 color = lerp(_WaterColor  , _TColor , saturate((depth - i.screenPos.w + _DepthOffset) * _Stretch)) * _Brightness;
			    float4 albedo = (color * color.a);// + bgcolor;

			    // standard settings for smoothness and metallic
			    //o.Smoothness = _Smoothness;
			    //o.Metallic = _Metallic;
			  
			    // planar reflections
			   // half4 rtReflections = tex2Dproj(_ReflectionTex, UNITY_PROJ_COORD(i.screenPos + noiseDist));
			   // rtReflections *= dot(i.normal, normal.y);
			    albedo += combinedFoam + colorRim + sparkle;
			   			   
			    return albedo;
			  
			   }
			   ENDCG
	   }
	}
	FallBack "Universal Render Pipeline/Simple Lit"
}