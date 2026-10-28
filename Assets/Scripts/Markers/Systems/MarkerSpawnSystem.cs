using Unity.Entities;
using Unity.Burst;
using Unity.Transforms;

[BurstCompile]
public partial class MarkerSpawnSystem : SystemBase 
{
    [BurstCompile]
    protected override void OnUpdate()
    {
        // Find entities that have a prefab reference but no marker instance
        EntityQuery query = GetEntityQuery(
            ComponentType.ReadOnly<MarkerPrefabReference>(),
            ComponentType.ReadOnly<MarkerOffset>(),
            ComponentType.ReadWrite<MarkerInstanceReference>());

        // Gather entities first
        using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);

        foreach (Entity owner in entities) 
        {
            MarkerInstanceReference instanceReference = EntityManager.GetComponentData<MarkerInstanceReference>(owner);

            // A marker has already been spawned
            if (instanceReference.value_ != Entity.Null && EntityManager.Exists(instanceReference.value_))
            {
                continue;
            }

            MarkerPrefabReference prefabReference = EntityManager.GetComponentData<MarkerPrefabReference>(owner);

            // Avoid instantiating an invalid prefab reference
            if (prefabReference.value_ == Entity.Null && !EntityManager.Exists(prefabReference.value_)) 
            {
                continue;
            }

            // Create a runtime instance of marker prefab
            Entity marker = EntityManager.Instantiate(prefabReference.value_);

            // Link the marker to the owner
            EntityManager.AddComponentData(marker, new MarkerOwner
            {
                value_ = owner
            });

            // Give the marker its configured vertical offset
            MarkerOffset offset = EntityManager.GetComponentData<MarkerOffset>(owner);
            EntityManager.AddComponentData(marker, offset);

            // Store the spawned marker so we dont spawn it again
            EntityManager.SetComponentData(owner, new MarkerInstanceReference
            {
                value_ = owner
            });

            // Start the marker at the owner's current position 
            if (EntityManager.HasComponent<LocalTransform>(owner) &&
                EntityManager.HasComponent<LocalTransform>(marker)) 
            {
                LocalTransform ownerTransform = EntityManager.GetComponentData<LocalTransform>(owner);

                LocalTransform markerTransform = EntityManager.GetComponentData<LocalTransform>(marker);

                markerTransform.Position = ownerTransform.Position + offset.value_;

                EntityManager.SetComponentData(marker, markerTransform);
            }
        }

    }
}