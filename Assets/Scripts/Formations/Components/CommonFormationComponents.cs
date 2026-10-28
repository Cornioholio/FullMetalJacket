using Unity.Entities;
using UnityEngine;

/// <summary>
/// Runtime squad assignment.
/// </summary>
public struct FormationAssignmentRequest : IComponentData
{
    public Entity value_;
}

/// <summary>
/// Records what squad a unit belongs to, owned by every unit.
/// </summary>
public struct FormationMembership : IComponentData
{
    public Entity value_;
}