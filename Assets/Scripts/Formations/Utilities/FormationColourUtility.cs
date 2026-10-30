using Unity.Mathematics;

/// <summary>
/// Generates consistent colours from team IDs
/// </summary>
public static class FormationColourUtility 
{
    public static float4 GetColour(byte teamID) 
    {
        // Cool method to distribute colour using golden ratio.
        float hue = (float)math.frac(teamID * 0.61);

        float3 k = new float3(1f, 2f / 3f, 1f / 3f);
        float3 p = math.abs(math.frac(hue + k) * 6f - 3f);

        float3 rgb = 0.95f * math.lerp(
            new float3(1f, 1f, 1f),
            math.clamp(p - 1f, 0f, 1f),
            0.75f);

        return new float4(rgb, 1f);
    }
}