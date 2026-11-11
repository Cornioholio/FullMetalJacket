
using Unity.Burst;
using Unity.Entities;

[BurstCompile]
[UpdateBefore(typeof(FormationLayoutSystem))]
public partial struct FormationChangeSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (request, combatState, squadFormation, settings) in
            SystemAPI.Query<
                RefRW<SquadFormationChangeRequest>,
                RefRO<SquadCombatState>,
                RefRW<SquadFormation>,
                RefRW<FormationSettings>>())
        {
            // Nothing to process.
            if (!request.ValueRO.pending_)
            {
                continue;
            }

            SquadFormationType requestedType =
                request.ValueRO.requestedType_;

            request.ValueRW.pending_ = false;

            // Formations cannot be changed during combat.
            if (combatState.ValueRO.inCombat_)
            {
                request.ValueRW.rejected_ = true;
                continue;
            }

            // Get the requested formation configuration.
            if (!TryGetSettings(requestedType, out FormationSettings newSettings))
            {
                request.ValueRW.rejected_ = true;
                continue;
            }

            // Apply the new formation.
            squadFormation.ValueRW.type_ = requestedType;
            settings.ValueRW = newSettings;

            request.ValueRW.rejected_ = false;
        }
    }

    private static bool TryGetSettings(
        SquadFormationType type,
        out FormationSettings settings)
    {
        settings = default;

        switch (type)
        {
            case SquadFormationType.Offensive:
            settings = new FormationSettings
            {
                layoutType_ = SquadFormationLayoutType.Wedge,
                spacing_ = 2.5f,
                depthSpacing_ = 2.5f,
                columns_ = 2,
                dirty_ = true
            };
            return true;

            case SquadFormationType.Defensive:
            settings = new FormationSettings
            {
                layoutType_ = SquadFormationLayoutType.Grid,
                spacing_ = 2.0f,
                depthSpacing_ = 2.0f,
                columns_ = 4,
                dirty_ = true
            };
            return true;

            case SquadFormationType.CQB:
            settings = new FormationSettings
            {
                layoutType_ = SquadFormationLayoutType.Grid,
                spacing_ = 1.0f,
                depthSpacing_ = 1.5f,
                columns_ = 2,
                dirty_ = true
            };
            return true;

            case SquadFormationType.DigIn:
            settings = new FormationSettings
            {
                layoutType_ = SquadFormationLayoutType.Grid,
                spacing_ = 3.0f,
                depthSpacing_ = 3.0f,
                columns_ = 4,
                dirty_ = true
            };
            return true;

            default:
                return false;
        }
    }
}
