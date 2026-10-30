using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Runtime squad assignment.
/// </summary>
public struct FormationAssignmentRequest : IComponentData
{
    public Entity value_;
}

/// <summary>
/// Records what formation a child formation (or unit) belongs to
/// </summary>
public struct FormationMembership : IComponentData
{
    public Entity value_;
}

/// <summary>
/// Child formation (or unit)'s assigned position in a formation
/// </summary>
public struct FormationOffset : IComponentData
{
    public float3 value_;
}