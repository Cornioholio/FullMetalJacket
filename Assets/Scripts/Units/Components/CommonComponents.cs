using Unity.Entities;
using Unity.Mathematics;

public struct Health : IComponentData
{
    public float value_;
}
public struct MovementSpeed : IComponentData
{
    public float value_;
}

public struct Destination : IComponentData 
{
    public float3 value_;
}
