using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class MarkerPrefabAuthoring : MonoBehaviour
{
    public GameObject markerPrefab;
    public Vector3 markerOffset = new Vector3(0f, 1.5f, 0f);

    private class Baker : Baker<MarkerPrefabAuthoring> 
    {
        public override void Bake(MarkerPrefabAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            // Store which prefab the entity should use for marker
            AddComponent(entity, new MarkerPrefabReference
            {
                value_ = GetEntity(authoring.markerPrefab, TransformUsageFlags.Dynamic)
            });

            // Store desired offset
            AddComponent(entity, new MarkerOffset
            {
                value_ = new float3(authoring.markerOffset.x, authoring.markerOffset.y, authoring.markerOffset.z)
            });

            AddComponent(entity, new MarkerInstanceReference
            {
                value_ = Entity.Null
            });
        }
    }
}