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
public enum SquadFormationType : byte 
{
    Offensive,
    Defensive,
    CQB,
    DigIn
}
public enum SquadFormationLayoutType : byte 
{
    Line,
    Wedge,
    Column,
    Grid
}
public struct FormationSettings : IComponentData 
{
    public SquadFormationLayoutType layoutType_;

    public float spacing_;
    public float depthSpacing_;

    public int columns_;

    // Requests a recalculation of formation destinations
    public bool dirty_;
}

public struct SquadCombatState : IComponentData 
{
    public bool inCombat_;
}
public struct SquadFormation : IComponentData 
{
    public SquadFormationType type_;
}
public struct SquadFormationChangeRequest : IComponentData 
{
    public SquadFormationType requestedType_;

    public bool pending_;

    public bool rejected_;
}