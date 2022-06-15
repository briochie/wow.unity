//UNITY_SHADER_NO_UPGRADE
#ifndef MYHLSLINCLUDE_INCLUDED
#define MYHLSLINCLUDE_INCLUDED

void PreLegion_float(float4 Map, float4 Layer_1, float4 Layer_2, float4 Layer_3, float4 Layer_0, out float4 Out)
{
	Out = Layer_3 * Map.b + Layer_2 * Map.g + Layer_1 * Map.r + Layer_0 * (1.0 - clamp(Map.r + Map.g + Map.b, 0, 1));
}
#endif //MYHLSLINCLUDE_INCLUDED
