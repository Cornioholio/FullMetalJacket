using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;

public class UnitCommandInput : MonoBehaviour 
{
    private EntityManager entityManager;
    private EntityQuery unitQuery;

    private void Start()
    {
        // Get ecs entity manager from world, then find units that can recieve commands.
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        unitQuery = entityManager.CreateEntityQuery(ComponentType.ReadWrite<Destination>(), ComponentType.ReadOnly<MovementSpeed>());
    }

    private void Update()
    {
        // Only process when lmb down.
        if (!Input.GetMouseButtonDown(0)) return; 

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        // Check hit
        if (Physics.Raycast(ray, out RaycastHit hitInfo)) 
        {
            // Convert hit point info into float
            float3 targetPosition = new float3(hitInfo.point.x, hitInfo.point.y, hitInfo.point.z);

            // Get all entities capable of recieving commands
            using NativeArray<Entity> entities = unitQuery.ToEntityArray(Allocator.Temp);

            // Give all matching units a destination
            foreach (Entity entity in entities) 
            {
                entityManager.SetComponentData(entity, new Destination
                {
                    value_ = targetPosition
                });
            }

            Debug.Log($"Move command issued to: {targetPosition}");
        }
    }
}