using Unity.Entities;
using Unity.Burst;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Rendering;

[BurstCompile]
public partial class MarkerSpawnSystem : SystemBase 
{
    [BurstCompile]
    protected override void OnUpdate()
    {
        // Find entities that have a prefab reference but no marker instance
        EntityQuery query = GetEntityQuery(
            ComponentType.ReadOnly<MarkerPrefabReference>(),
            ComponentType.ReadWrite<MarkerInstanceReference>());

        // Gather entities first
        using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);

        foreach (Entity owner in entities) 
        {
            using var prefabReferences = EntityManager.GetBuffer<MarkerPrefabReference>(owner).ToNativeArray(Unity.Collections.Allocator.Temp);

            DynamicBuffer<MarkerInstanceReference> instanceBuffer = EntityManager.GetBuffer<MarkerInstanceReference>(owner);
            
            while (instanceBuffer.Length < prefabReferences.Length) 
            {
                instanceBuffer.Add(new MarkerInstanceReference
                {
                    value_ = Entity.Null,
                    instance_ = Entity.Null
                });
            }

            for (int i = 0; i < prefabReferences.Length; i++) 
            {
                MarkerPrefabReference prefabReference = prefabReferences[i];

                if (prefabReference.value_ == Entity.Null ||
                    !EntityManager.Exists(prefabReference.value_)) 
                {
                    continue;
                }

                instanceBuffer = EntityManager.GetBuffer<MarkerInstanceReference>(owner);

                MarkerInstanceReference instanceReference = instanceBuffer[i];

                if (instanceReference.value_ == prefabReference.value_ && 
                    instanceReference.instance_ != Entity.Null &&
                    EntityManager.Exists(instanceReference.instance_)) 
                {
                    continue;
                }

                if (instanceReference.instance_ != Entity.Null && 
                    EntityManager.Exists(instanceReference.instance_ )) 
                {
                    EntityManager.DestroyEntity(instanceReference.instance_);
                }

                Entity marker = EntityManager.Instantiate(prefabReference.value_);

                if (!EntityManager.HasComponent<URPMaterialPropertyBaseColor>(marker)) 
                {
                    EntityManager.AddComponentData(marker, new URPMaterialPropertyBaseColor
                    {
                        Value = new float4(0.75f, 0.75f, 0.75f, 1f)
                    });
                }

                MarkerOwner markerOwner = new MarkerOwner
                {
                    value_ = owner,
                    type_ = prefabReference.type_
                };

                if (EntityManager.HasComponent<MarkerOwner>(marker)) 
                {
                    EntityManager.SetComponentData(marker, markerOwner);
                } else 
                {
                    EntityManager.AddComponentData(marker, markerOwner);
                }

                MarkerOffset markerOffset = new MarkerOffset
                {
                    value_ = prefabReference.offset_
                };

                if (EntityManager.HasComponent<MarkerOffset>(marker)) 
                {
                    EntityManager.SetComponentData(marker, markerOffset);
                } else 
                {
                    EntityManager.AddComponentData(marker, markerOffset);
                }

                instanceBuffer = EntityManager.GetBuffer<MarkerInstanceReference>(owner);

                instanceBuffer[i] = new MarkerInstanceReference
                {
                    value_ = prefabReference.value_,
                    instance_ = marker
                };

                if (EntityManager.HasComponent<LocalTransform>(owner) &&
                    EntityManager.HasComponent<LocalTransform>(marker)) 
                {
                    LocalTransform ownerTransform = EntityManager.GetComponentData<LocalTransform>(owner);

                    LocalTransform markerTransform = EntityManager.GetComponentData<LocalTransform>(marker);

                    markerTransform.Position = ownerTransform.Position + prefabReference.offset_;

                    EntityManager.SetComponentData(marker, markerTransform);
                }
            }
        }

    }
}