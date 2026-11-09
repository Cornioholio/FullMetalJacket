using Unity.Entities;
using Unity.Mathematics;

public enum MarkerType 
{
    DirectionSelection,
    Squad
}
/// <summary>
/// Identifies the entity the marker follows.
/// </summary>
public struct MarkerOwner : IComponentData 
{
    public Entity value_;
    public MarkerType type_;
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
public struct MarkerPrefabReference : IBufferElementData
{
    public Entity value_;
    public float3 offset_;
    public MarkerType type_;
}

/// <summary>
/// Stored the marker instance associated with an entitya
/// </summary>
public struct MarkerInstanceReference : IBufferElementData 
{
    public Entity value_;
    public Entity instance_;
}