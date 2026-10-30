using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct FormationLayoutSystem : ISystem 
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state) 
    {
        foreach (var (movementOrder, squadMembers) in
            SystemAPI.Query<RefRW<SquadMovementOrder>, DynamicBuffer<SquadMember>>()) 
        {
            // Ignore squads that have no active move order
            if (!movementOrder.ValueRO.active_) 
            {
                continue;
            }

            int memberCount = squadMembers.Length;

            // Nothing to move
            if (memberCount == 0)
            {
                movementOrder.ValueRW.active_ = false;
                continue;
            }

            // temporary and replaced with standard formations as player's discretion
            float spacing = 2f;

            // Total width of formation
            float formationWidth = (memberCount - 1f) * spacing;

            // Calculate starting position relative to the centre
            float startOffset = -formationWidth * 0.5f;

            for (int i = 0; i < memberCount; i++) 
            {
                Entity unit = squadMembers[i].value_;
                
                // Skip invalid entities
                if (!SystemAPI.Exists(unit) ||
                    !SystemAPI.HasComponent<Destination>(unit) ||
                    !SystemAPI.HasComponent<LocalTransform>(unit)) 
                {
                    continue;
                }

                float offset = startOffset + i * spacing;

                float3 unitDestination = movementOrder.ValueRO.destination_;

                // Position units across the formation, preserve y 
                unitDestination.x += offset;
                unitDestination.y = SystemAPI.GetComponent<LocalTransform>(unit).Position.y;

                SystemAPI.SetComponent(unit, new Destination
                {
                    value_ = unitDestination,
                });
            }
            movementOrder.ValueRW.active_ = false;
        }
    }
}