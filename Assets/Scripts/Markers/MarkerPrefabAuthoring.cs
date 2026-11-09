using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using JetBrains.Annotations;
using System.Collections.Generic;

public class MarkerPrefabAuthoring : MonoBehaviour
{
    [System.Serializable]
    public class MarkerEntry 
    {
        public GameObject markerPrefab;
        public MarkerType markerType;
        public Vector3 markerOffset;
    }

    public List<MarkerEntry> markers = new List<MarkerEntry>();

    private class Baker : Baker<MarkerPrefabAuthoring> 
    {
        public override void Bake(MarkerPrefabAuthoring authoring)
        {   
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            DynamicBuffer<MarkerPrefabReference> prefabBuffer = AddBuffer<MarkerPrefabReference>(entity);

            AddBuffer<MarkerInstanceReference>(entity);

            foreach (MarkerEntry marker in authoring.markers) 
            {
                if (marker.markerPrefab == null)
                {
                    continue;
                }

                prefabBuffer.Add(new MarkerPrefabReference
                {
                    value_ = GetEntity(marker.markerPrefab, TransformUsageFlags.Dynamic),
                    offset_ = new float3(
                        marker.markerOffset.x,
                        marker.markerOffset.y,
                        marker.markerOffset.z),
                    type_ = marker.markerType
                });
            } 
        }
    }
}