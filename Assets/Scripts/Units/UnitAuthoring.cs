using UnityEngine;
using Unity.Entities;

public class UnitAuthoring : MonoBehaviour
{
    public float health = 100f;
    public float movementSpeed = 5f;

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
        }
    }
}
