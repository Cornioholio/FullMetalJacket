using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct MarkerFollowSystem : ISystem 
{

    // Read each markers owner and offset, then update position
    [BurstCompile]
    public void OnUpdate(ref SystemState state) 
    {
        foreach (var (markerOwner, markerOffset, markerTransform)
                in SystemAPI.Query
                <
                 RefRO<MarkerOwner>,
                 RefRO<MarkerOffset>,
                 RefRW<LocalTransform>
                >())
        {
            Entity owner = markerOwner.ValueRO.value_;

            // Skip markers whose owner doesnt exist
            if (!SystemAPI.Exists(owner)) 
            {
                continue;
            }

            // Follow owners with a transform
            if (!SystemAPI.HasComponent<LocalTransform>(owner)) 
            {
                continue;
            }

            LocalTransform ownerTransform = SystemAPI.GetComponent<LocalTransform>(owner);

            markerTransform.ValueRW.Position = ownerTransform.Position + markerOffset.ValueRO.value_;
        }
    }
}