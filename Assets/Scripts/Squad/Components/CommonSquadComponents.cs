using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;

/// <summary>
/// Squad identification
/// </summary>
public struct Squad : IComponentData 
{
    public FixedString64Bytes name_;
    public byte teamID_;
    public float4 colour_;
}

/// <summary>
/// Squad's dynamic buffer of total units
/// </summary>
public struct SquadMember : IBufferElementData 
{
    public Entity value_;
}

/// <summary>
/// Current movement order on squad level
/// </summary>
public struct SquadMovementOrder : IComponentData 
{
    public float3 destination_;
    public bool active_;
}