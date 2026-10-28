using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Identifies the entity the marker follows.
/// </summary>
public struct MarkerOwner : IComponentData 
{
    public Entity value_;
}

/// <summary>
/// Position offset relative to the owner
/// </summary>
public struct MarkerOffset : IComponentData 
{
    public float3 value_;
}

/// <summary>
/// References the marker prefab used by an entity.
/// </summary>
public struct MarkerPrefabReference : IComponentData 
{
    public Entity value_;
}

/// <summary>
/// Stored the marker instance associated with an entity
/// </summary>
public struct MarkerInstanceReference : IComponentData 
{
    public Entity value_;
}