using UnityEngine;
using Unity.Entities;
using Unity.Collections;

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
                teamID_ = authoring.teamID
            });

            AddBuffer<SquadMember>(squadEntity);
        }
    }
}
