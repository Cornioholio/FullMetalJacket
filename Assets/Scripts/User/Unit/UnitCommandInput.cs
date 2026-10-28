using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

public class UnitCommandInput : MonoBehaviour 
{
    private World entityWorld;
    private bool isInitialised;

    private EntityManager entityManager;
    private EntityQuery unitQuery;
    private EntityQuery squadQuery;
    private EntityQuery physicsWorldQuery;

    private Entity selectedEntity = Entity.Null;

    private void InitialiseECS()
    {
        entityWorld = World.DefaultGameObjectInjectionWorld;

        // The ECS world might not exist yet.
        // Update() will retry until it becomes available.
        if (entityWorld == null || !entityWorld.IsCreated)
        {
            return;
        }

        entityManager = entityWorld.EntityManager;

        unitQuery = entityManager.CreateEntityQuery(
            ComponentType.ReadWrite<Destination>(),
            ComponentType.ReadOnly<MovementSpeed>());

        physicsWorldQuery = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<PhysicsWorldSingleton>());

        squadQuery = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<Squad>(), 
            ComponentType.ReadOnly<SquadMember>());


        isInitialised = true;

        Debug.Log("UnitCommandInput initialised successfully.");
    }
    private void Start()
    {
        InitialiseECS();
    }
    private void OnDestroy()
    {
        if (!isInitialised)
        {
            return;
        }
        if (entityWorld != null && entityWorld.IsCreated)
        {
            unitQuery.Dispose();
            physicsWorldQuery.Dispose();
            squadQuery.Dispose();
        }
    }
    private void AssignSelectedUnitToSquad()
    {
        if (selectedEntity == Entity.Null ||
            !entityManager.Exists(selectedEntity))
        {
            Debug.LogWarning("Select a unit before assigning it to a squad.");
            return;
        }

        if (squadQuery.IsEmptyIgnoreFilter)
        {
            Debug.LogWarning("No squad entities found.");
            return;
        }

        // Find the squad entity.
        using var squads = squadQuery.ToEntityArray(Allocator.Temp);

        Entity squadEntity = Entity.Null;

        foreach (Entity squad in squads)
        {
            Squad squadData = entityManager.GetComponentData<Squad>(squad);

            if (squadData.name_.ToString() == "Alpha Squad")
            {
                squadEntity = squad;
                break;
            }
        }

        if (squadEntity == Entity.Null)
        {
            Debug.LogWarning("Alpha Squad was not found.");
            return;
        }

        // Ensure the selected unit has membership data.
        if (!entityManager.HasComponent<SquadMembership>(selectedEntity))
        {
            Debug.LogError(
                "Selected unit has no SquadMembership component. " +
                "Check UnitAuthoring's Baker.");
            return;
        }

        // Create or update the assignment request.
        var request = new SquadAssignmentRequest
        {
            value_ = squadEntity
        };

        if (entityManager.HasComponent<SquadAssignmentRequest>(selectedEntity))
        {
            entityManager.SetComponentData(selectedEntity, request);
        }
        else
        {
            entityManager.AddComponentData(selectedEntity, request);
        }

        Debug.Log($"Requested assignment of {selectedEntity} to Alpha Squad.");
    }
    private void SelectUnit()
    {
        Debug.Log("SelectUnit started.");

        // Clear the previous selection first.
        if (selectedEntity != Entity.Null &&
            entityManager.Exists(selectedEntity))
        {
            if (entityManager.HasComponent<Selected>(selectedEntity))
            {
                entityManager.RemoveComponent<Selected>(selectedEntity);
            }
        }

        selectedEntity = Entity.Null;

        Camera mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError("No MainCamera found.");
            return;
        }

        if (physicsWorldQuery.IsEmptyIgnoreFilter)
        {
            Debug.LogError("ECS Physics World is missing.");
            return;
        }

        UnityEngine.Ray ray =
            mainCamera.ScreenPointToRay(Input.mousePosition);

        PhysicsWorldSingleton physicsWorld =
            physicsWorldQuery.GetSingleton<PhysicsWorldSingleton>();

        RaycastInput raycastInput = new RaycastInput
        {
            Start = ray.origin,
            End = ray.origin + ray.direction * 1000f,
            Filter = new CollisionFilter
            {
                BelongsTo = ~0u,
                CollidesWith = ~0u,
                GroupIndex = 0
            }
        };

        // Cast the ray into the ECS physics world.
        if (!physicsWorld.CollisionWorld.CastRay(
            raycastInput, out Unity.Physics.RaycastHit hit))
        {
            Debug.Log("Selection cleared.");
            return;
        }

        Entity hitEntity = hit.Entity;

        // Only select entities that can receive movement commands.
        if (!entityManager.Exists(hitEntity) ||
            !entityManager.HasComponent<Destination>(hitEntity) ||
            !entityManager.HasComponent<MovementSpeed>(hitEntity))
        {
            Debug.Log("Selection cleared.");
            return;
        }

        selectedEntity = hitEntity;

        // Mark the newly selected unit.
        if (!entityManager.HasComponent<Selected>(selectedEntity))
        {
            entityManager.AddComponent<Selected>(selectedEntity);
        }

        Debug.Log($"Selected unit: {selectedEntity}");
    }
    private void MoveSelectedUnit()
    {
        // Do nothing if no valid unit is selected.
        if (selectedEntity == Entity.Null ||
            !entityManager.Exists(selectedEntity))
        {
            Debug.Log("No valid unit selected.");
            return;
        }

        Debug.Log($"Selected entity: {selectedEntity}");

        Camera mainCamera = Camera.main;

        if (mainCamera == null)
        {
            return;
        }

        UnityEngine.Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        // Use the existing GameObject physics raycast for the ground.
        if (Physics.Raycast(ray, out UnityEngine.RaycastHit hitInfo))
        {
            float3 targetPosition = new float3(
                hitInfo.point.x,
                entityManager.GetComponentData<LocalTransform>(selectedEntity).Position.y,
                hitInfo.point.z);

            // Only update the selected unit's destination.
            if (entityManager.HasComponent<Destination>(selectedEntity))
            {
                entityManager.SetComponentData(
                    selectedEntity,
                    new Destination
                    {
                        value_ = targetPosition
                    });

                Debug.Log($"Move command issued to: {targetPosition}");
            }
        }
    }

    private void Update()
    {
        if (!isInitialised) 
        {
            InitialiseECS();

            if (!isInitialised) 
            {
                return;
            }
        }

        // G assigns unit to alpha squad
        if (Input.GetKeyDown(KeyCode.G)) 
        {
            AssignSelectedUnitToSquad();
        }

        // Right-click selects a unit.
        if (Input.GetMouseButtonDown(1))
        {
            SelectUnit();
        }

        // Left-click moves the selected unit.
        if (Input.GetMouseButtonDown(0))
        {
            MoveSelectedUnit();
        }
    }
}