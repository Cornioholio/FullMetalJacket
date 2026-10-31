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
    private void AssignSelectedUnitToSquad(byte teamID)
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

            if (squadData.teamID_ == teamID)
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
        if (!entityManager.HasComponent<FormationMembership>(selectedEntity))
        {
            Debug.LogError(
                "Selected unit has no FormationMembership component. " +
                "Check UnitAuthoring's Baker.");
            return;
        }

        // Create or update the assignment request.
        var request = new FormationAssignmentRequest
        {
            value_ = squadEntity
        };

        if (entityManager.HasComponent<FormationAssignmentRequest>(selectedEntity))
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
        Debug.Log($"Physics bodies: {physicsWorld.CollisionWorld.NumBodies}");
        Debug.Log($"Ray origin: {ray.origin}");
        Debug.Log($"Ray direction: {ray.direction}");

        // Cast the ray into the ECS physics world.
        if (!physicsWorld.CollisionWorld.CastRay(
            raycastInput, out Unity.Physics.RaycastHit hit))
        {
            Debug.LogWarning("RAYCAST FAILED: No ECS physics body was hit.");
            return;
        }

        Entity hitEntity = hit.Entity;

        Debug.Log($"Hit entity: {hitEntity}");

        Debug.Log($"Has LocalTransform: {entityManager.HasComponent<LocalTransform>(hitEntity)}");
        Debug.Log($"Has Health: {entityManager.HasComponent<Health>(hitEntity)}");
        Debug.Log($"Has SquadMarker: {entityManager.HasComponent<SquadMarker>(hitEntity)}");
        Debug.Log($"Has FormationMembership: {entityManager.HasComponent<FormationMembership>(hitEntity)}");

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
            Debug.LogError("MOVEMENT FAILED: No main camera found.");
            return;
        }

        Debug.Log("Movement camera found.");

        UnityEngine.Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        PhysicsWorldSingleton physicsWorld = physicsWorldQuery.GetSingleton<PhysicsWorldSingleton>();

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

        // Use the existing GameObject physics raycast for the ground.
        if (physicsWorld.CollisionWorld.CastRay(raycastInput, out Unity.Physics.RaycastHit hitInfo))
        {

            float3 targetPosition = new float3(
                hitInfo.Position.x,
                entityManager.GetComponentData<LocalTransform>(selectedEntity).Position.y,
                hitInfo.Position.z);

            // Check if selected is a 
            if (entityManager.HasComponent<FormationMembership>(selectedEntity)) 
            {
                Debug.Log($"Has FormationMembership: {entityManager.HasComponent<FormationMembership>(selectedEntity)}");
                FormationMembership membership = entityManager.GetComponentData<FormationMembership>(selectedEntity);
                Debug.Log($"Squad entity: {membership.value_}");
                Entity squadEntity = membership.value_;

                // Validate squad membership
                if (squadEntity != Entity.Null &&
                    entityManager.Exists(squadEntity) &&
                    entityManager.HasComponent<SquadMovementOrder>(squadEntity))
                {
                    SquadMovementOrder movementOrder = entityManager.GetComponentData<SquadMovementOrder>(squadEntity);

                    movementOrder.destination_ = targetPosition;
                    movementOrder.active_ = true;

                    entityManager.SetComponentData(squadEntity, movementOrder);

                    Debug.Log($"Squad movement order issued to: {targetPosition}");

                    return;
                }
            }

            // Otherwise move individual unit.
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
        } else
        {
            Debug.LogWarning("MOVEMENT FAILED: Ground raycast did not hit anything.");
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
            AssignSelectedUnitToSquad(0);
        }
        // F assigns unit to alpha squad
        if (Input.GetKeyDown(KeyCode.F))
        {
            AssignSelectedUnitToSquad(1);
        }
        // Right-click selects a unit.
        if (Input.GetMouseButtonDown(1))
        {
            Debug.Log("RIGHT CLICK DETECTED");
            SelectUnit();
        }

        // Left-click moves the selected unit.
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("LEFT CLICK DETECTED");
            MoveSelectedUnit();
        }
    }
}