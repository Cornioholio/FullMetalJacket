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

        foreach (var (transform, speed, destination, facing) in SystemAPI.Query<
            RefRW<LocalTransform>, 
            RefRO<MovementSpeed>, 
            RefRO<Destination>,
            RefRW<FacingDirection>>())
        {
            float step = speed.ValueRO.value_ * deltaTime;

            float3 currentPosition = transform.ValueRO.Position;
            float3 targetPosition = destination.ValueRO.value_;

            float3 direction = targetPosition - currentPosition;
            direction.y = 0f;

            float distance = math.length(direction);

            if (distance > 0.01f)
            {
                // Calculate and apply facing rotation.
                float3 facingDirection = math.normalize(direction);

                quaternion targetRotation = quaternion.LookRotationSafe(
                    facingDirection,
                    math.up());

                facing.ValueRW.value_ = targetRotation;
                transform.ValueRW.Rotation = targetRotation;

                // Move towards destination.
                float3 movement = facingDirection * math.min(step, distance);

                transform.ValueRW.Position += movement;
            }
        }
    }
}
