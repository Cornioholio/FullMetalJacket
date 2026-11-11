
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct FormationLayoutSystem : ISystem
{
    private const float DirectionThreshold = 0.0001f;

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (movementOrder, settings, squadMembers) in
            SystemAPI.Query<
                RefRW<SquadMovementOrder>,
                RefRW<FormationSettings>,
                DynamicBuffer<SquadMember>>())
        {
            bool hasMovementOrder = movementOrder.ValueRO.active_;
            bool needsLayout = settings.ValueRO.dirty_;

            // Only process squads that need updating.
            if (!hasMovementOrder && !needsLayout)
            {
                continue;
            }

            float3 squadCentre = float3.zero;
            float3 averageFacing = float3.zero;

            int validMemberCount = 0;
            int facingCount = 0;

            foreach (SquadMember member in squadMembers)
            {
                Entity unit = member.value_;

                if (!SystemAPI.Exists(unit) ||
                    !SystemAPI.HasComponent<Destination>(unit) ||
                    !SystemAPI.HasComponent<LocalTransform>(unit))
                {
                    continue;
                }

                LocalTransform transform =
                    SystemAPI.GetComponent<LocalTransform>(unit);

                squadCentre += transform.Position;
                validMemberCount++;

                // Collect facing directions for stationary
                // formation changes.
                if (SystemAPI.HasComponent<FacingDirection>(unit))
                {
                    FacingDirection facing =
                        SystemAPI.GetComponent<FacingDirection>(unit);

                    float3 forward = math.rotate(
                        facing.value_,
                        new float3(0f, 0f, 1f));

                    forward.y = 0f;

                    if (math.lengthsq(forward) > DirectionThreshold)
                    {
                        averageFacing += math.normalize(forward);
                        facingCount++;
                    }
                }
            }

            // Nothing to move or arrange.
            if (validMemberCount == 0)
            {
                if (hasMovementOrder)
                {
                    movementOrder.ValueRW.active_ = false;
                }

                continue;
            }

            squadCentre /= validMemberCount;

            float3 movementDirection;

            if (hasMovementOrder)
            {
                movementDirection =
                    movementOrder.ValueRO.destination_ - squadCentre;
            }
            else
            {
                movementDirection = averageFacing;
            }

            movementDirection.y = 0f;

            // Avoid normalising a zero-length vector.
            if (math.lengthsq(movementDirection) > DirectionThreshold)
            {
                movementDirection = math.normalize(movementDirection);
            }
            else if (facingCount > 0 &&
                     math.lengthsq(averageFacing) > DirectionThreshold)
            {
                movementDirection = math.normalize(averageFacing);
            }
            else
            {
                movementDirection = new float3(0f, 0f, 1f);
            }

            // Perpendicular horizontal formation axis.
            float3 formationRight = new float3(
                movementDirection.z,
                0f,
                -movementDirection.x);

            // Use current squad centre if only rearranging formation.
            float3 formationCentre = hasMovementOrder
                ? movementOrder.ValueRO.destination_
                : squadCentre;

            SquadFormationLayoutType layoutType =
                settings.ValueRO.layoutType_;

            float spacing = settings.ValueRO.spacing_;
            float depthSpacing = settings.ValueRO.depthSpacing_;

            int columns = math.max(1, settings.ValueRO.columns_);

            int formationIndex = 0;

            foreach (SquadMember member in squadMembers)
            {
                Entity unit = member.value_;

                if (!SystemAPI.Exists(unit) ||
                    !SystemAPI.HasComponent<Destination>(unit) ||
                    !SystemAPI.HasComponent<LocalTransform>(unit))
                {
                    continue;
                }

                LocalTransform transform =
                    SystemAPI.GetComponent<LocalTransform>(unit);

                float sideOffset = 0f;
                float depthOffset = 0f;

                switch (layoutType)
                {
                    case SquadFormationLayoutType.Line:
                    {
                        sideOffset =
                            formationIndex - (validMemberCount - 1f) * 0.5f;

                        break;
                    }
                    case SquadFormationLayoutType.Wedge:
                    {
                        if (formationIndex == 0)
                        {
                            // First soldier occupies the front.
                            sideOffset = 0f;
                            depthOffset = 0f;
                        }
                        else
                        {
                            int row = (formationIndex + 1) / 2;

                            bool unpairedLast =
                                formationIndex == validMemberCount - 1 &&
                                formationIndex % 2 == 1;

                            if (unpairedLast)
                            {
                                // An unmatched soldier occupies
                                // the centre of the rear rank.
                                sideOffset = 0f;
                            }
                            else
                            {
                                sideOffset = formationIndex % 2 == 1
                                    ? -row * 0.5f
                                    : row * 0.5f;
                            }

                            depthOffset = row;
                        }

                        break;
                    }
                    case SquadFormationLayoutType.Column:
                    {
                        depthOffset = formationIndex;
                        sideOffset = 0f;

                        break;
                    }
                    case SquadFormationLayoutType.Grid:
                    {
                        int row = formationIndex / columns;
                        int column = formationIndex % columns;

                        // Centre incomplete rows independently.
                        int rowStart = row * columns;

                        int rowMemberCount = math.min(
                            columns,
                            validMemberCount - rowStart);

                        sideOffset =
                            column - (rowMemberCount - 1f) * 0.5f;

                        depthOffset = row;

                        break;
                    }
                }

                formationIndex++;

                // Calculate final world-space destination.
                float3 unitDestination =
                    formationCentre
                    + formationRight * (sideOffset * spacing)
                    - movementDirection * (depthOffset * depthSpacing);

                // Preserve the soldier's current height.
                unitDestination.y = transform.Position.y;

                SystemAPI.SetComponent(unit, new Destination
                {
                    value_ = unitDestination
                });
            }
            movementOrder.ValueRW.active_ = false;
            settings.ValueRW.dirty_ = false;
        }
    }
}
