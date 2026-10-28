using Unity.Entities;
using Unity.Mathematics;

// !! Components given to ALL units by default live here !!

public struct Health : IComponentData
{
    public float value_;
}


// MOVEMENT --------------------------------------------------
public struct MovementSpeed : IComponentData
{
    public float value_;
}

/// <summary>
/// Holds unit target move destination given by commander.
/// </summary>
public struct Destination : IComponentData 
{
    public float3 value_;
}

/// <summary>
/// Tag component, ownership determines whether or not unit is selected by commander.
/// </summary>
public struct Selected : IComponentData 
{

}


// SQUADS ----------------------------------------------------

/// <summary>
/// Records what squad a unit belongs to, owned by every unit.
/// </summary>
public struct SquadMembership : IComponentData
{
    public Entity value_;
}

/// <summary>
/// Runtime squad assignment.
/// </summary>
public struct SquadAssignmentRequest : IComponentData
{
    public Entity value_;
}