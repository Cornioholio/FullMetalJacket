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


public struct SquadMarker : IComponentData 
{
    public Entity value_;
}