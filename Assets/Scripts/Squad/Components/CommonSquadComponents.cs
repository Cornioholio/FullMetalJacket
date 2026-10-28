using Unity.Entities;
using Unity.Collections;

/// <summary>
/// Squad identification
/// </summary>
public struct Squad : IComponentData 
{
    public FixedString64Bytes name_;
    public byte teamID_;
}

/// <summary>
/// Squad's dynamic buffer of total units
/// </summary>
public struct SquadMember : IBufferElementData 
{
    public Entity value_;
}
