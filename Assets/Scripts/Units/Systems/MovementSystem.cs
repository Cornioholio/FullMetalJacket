using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct MovementSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state) 
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (transform, speed) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<MovementSpeed>>())
        {
            transform.ValueRW.Position += new float3(0f, 0f, speed.ValueRO.value_ * deltaTime);
        }
    }
}
