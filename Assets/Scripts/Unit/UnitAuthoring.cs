using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class UnitAuthoring : MonoBehaviour
{
    public float health = 100f;
    public float movementSpeed = 5f;
    public Vector3 destination = Vector3.zero;

    private class UnitBaker : Baker<UnitAuthoring> 
    {
        public override void Bake(UnitAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new Health
            {
                value_ = authoring.health
            });
            AddComponent(entity, new MovementSpeed 
            {
                value_ = authoring.movementSpeed
            });
            AddComponent(entity, new Destination
            {
                value_ = authoring.transform.position
            });

            AddComponent(entity, new FormationMembership 
            {
                value_ = Entity.Null
            });
        }
    }
}
