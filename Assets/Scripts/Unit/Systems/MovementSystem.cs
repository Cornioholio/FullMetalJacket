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

        foreach (var (transform, speed, destination) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<MovementSpeed>, RefRO<Destination>>())
        {
            float step = speed.ValueRO.value_ * deltaTime;

            float3 currentPosition = transform.ValueRO.Position;
            float3 targetPosition = destination.ValueRO.value_;

            float3 direction = targetPosition - currentPosition;
            float distance = math.length(direction);

            if (distance > 0.01f)
            {
                float3 movement = math.normalize(direction) * math.min(step, distance);

                transform.ValueRW.Position += movement;
            }
        }
    }
}
