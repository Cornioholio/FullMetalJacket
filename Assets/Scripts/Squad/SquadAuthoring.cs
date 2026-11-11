using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;

public class SquadAuthoring : MonoBehaviour
{
    public string squadName = "Alpha Squad";
    public byte teamID = 0;

    private class Baker : Baker<SquadAuthoring> 
    {
        public override void Bake(SquadAuthoring authoring)
        {
            Entity squadEntity = GetEntity(TransformUsageFlags.None);

            AddComponent(squadEntity, new Squad
            {
                name_ = new FixedString64Bytes(authoring.squadName),
                teamID_ = authoring.teamID,
                colour_ = FormationColourUtility.GetColour(authoring.teamID)
            });
            AddComponent(squadEntity, new SquadMovementOrder
            {
                destination_ = float3.zero,
                active_ = false
            });
            AddComponent(squadEntity, new SquadFormationChangeRequest
            {
                requestedType_ = SquadFormationType.Defensive,
                pending_ = false,
                rejected_ = false
            });
            AddComponent(squadEntity, new SquadFormation
            {
                type_ = SquadFormationType.Defensive
            });
            AddComponent(squadEntity, new FormationSettings
            {
                layoutType_ = SquadFormationLayoutType.Grid,
                spacing_ = 2f,
                depthSpacing_ = 2f,
                columns_ = 4,
                dirty_ = true
            });
            AddComponent(squadEntity, new SquadCombatState
            {
                inCombat_ = false
            });

            AddBuffer<SquadMember>(squadEntity);
        }
    }
}
