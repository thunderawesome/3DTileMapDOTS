void CurveWorld_float(in float4 VertexPos, in float3 BendAmount, in float3 BendOrigin,	in float BendFallOff, in float BendFallOffStr, out float4 PositionOutput)
{
	// set the shader graph node previews
	#ifdef SHADERGRAPH_PREVIEW
		BendAmount = float3(0,0,0);
		BendOrigin = float3(0,0,0);
		BendFallOff = 10.0;
		BendFallOffStr = 2.25;
		PositionOutput = float4(0,0,0,0);
		VertexPos = float4(0,0,0,0);
	#endif

	float4 world = mul(unity_ObjectToWorld, VertexPos);
	float dist = length(world.xyz - BendOrigin.xyz);
	dist = max(0, dist - BendFallOff);

	dist = pow(dist, BendFallOffStr);
	world.xyz += dist * BendAmount;
	PositionOutput = mul(unity_WorldToObject, world);
}