using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

public class UnitCommandInput : MonoBehaviour 
{
    private enum CommandMode 
    {
        Individual,
        Squad
    }

    private CommandMode commandMode = CommandMode.Individual;

    private World entityWorld;
    private bool isInitialised;

    private EntityManager entityManager;
    private EntityQuery unitQuery;
    private EntityQuery squadQuery;
    private EntityQuery physicsWorldQuery;
    private Entity pendingFormationSquad = Entity.Null;
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

            if (commandMode == CommandMode.Squad &&
                entityManager.HasComponent<FormationMembership>(selectedEntity))
            {
                FormationMembership membership =
                    entityManager.GetComponentData<FormationMembership>(selectedEntity);

                Entity squadEntity = membership.value_;

                // Validate squad membership and movement order.
                if (squadEntity != Entity.Null &&
                    entityManager.Exists(squadEntity) &&
                    entityManager.HasComponent<SquadMovementOrder>(squadEntity))
                {
                    SquadMovementOrder movementOrder =
                        entityManager.GetComponentData<SquadMovementOrder>(squadEntity);

                    movementOrder.destination_ = targetPosition;
                    movementOrder.active_ = true;

                    entityManager.SetComponentData(squadEntity, movementOrder);

                    Debug.Log($"Squad movement order issued to: {targetPosition}");
                    return;
                }

                Debug.LogWarning("Selected unit has no valid squad movement order.");
            }

            // Only the selected soldier receives this destination.
            if (entityManager.HasComponent<Destination>(selectedEntity))
            {
                entityManager.SetComponentData(
                    selectedEntity,
                    new Destination
                    {
                        value_ = targetPosition
                    });

                Debug.Log($"Individual movement order issued to: {targetPosition}");
            }
        } else
        {
            Debug.LogWarning("MOVEMENT FAILED: Ground raycast did not hit anything.");
        }

    }
    private void RequestSquadFormationChange(SquadFormationType requestedType)
    {
        // Ensure a valid soldier is selected.
        if (selectedEntity == Entity.Null ||
            !entityManager.Exists(selectedEntity))
        {
            Debug.LogWarning("Select a squad member first.");
            return;
        }

        // The selected soldier must belong to a squad.
        if (!entityManager.HasComponent<FormationMembership>(selectedEntity))
        {
            Debug.LogWarning("Selected unit does not belong to a squad.");
            return;
        }

        FormationMembership membership =
            entityManager.GetComponentData<FormationMembership>(selectedEntity);

        Entity squadEntity = membership.value_;

        if (squadEntity == Entity.Null ||
            !entityManager.Exists(squadEntity))
        {
            Debug.LogWarning("Selected unit has invalid squad membership.");
            return;
        }

        // Ensure the squad supports formation changes.
        if (!entityManager.HasComponent<SquadFormationChangeRequest>(squadEntity))
        {
            Debug.LogError("Squad is missing FormationChangeRequest."); // HERE!!!!
            return;
        }

        SquadFormationChangeRequest request =
            entityManager.GetComponentData<SquadFormationChangeRequest>(squadEntity);

        request.requestedType_ = requestedType;
        request.pending_ = true;
        request.rejected_ = false;

        entityManager.SetComponentData(squadEntity, request);

        pendingFormationSquad = squadEntity;

        Debug.Log($"Requested formation change: {requestedType}");
    }
    private void CheckFormationChangeResult()
    {
        if (pendingFormationSquad == Entity.Null)
        {
            return;
        }

        if (!entityManager.Exists(pendingFormationSquad))
        {
            pendingFormationSquad = Entity.Null;
            return;
        }

        SquadFormationChangeRequest request =
            entityManager.GetComponentData<SquadFormationChangeRequest>(
                pendingFormationSquad);

        // Wait for FormationChangeSystem to process the request.
        if (request.pending_)
        {
            return;
        }

        if (request.rejected_)
        {
            Debug.LogWarning(
                "FORMATION REJECTED: Squad is in combat or formation is invalid.");

            request.rejected_ = false;

            entityManager.SetComponentData(
                pendingFormationSquad,
                request);
        }
        else
        {
            Debug.Log("Formation successfully changed.");
        }

        pendingFormationSquad = Entity.Null;
    }
    private void ToggleCommandMode() 
    {
        commandMode = commandMode == CommandMode.Individual ? CommandMode.Squad : CommandMode.Individual;

        Debug.Log($"Command mode changed to: {commandMode}");
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
        CheckFormationChangeResult();

        if (Input.GetKeyDown(KeyCode.Tab)) 
        {
            ToggleCommandMode();
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
        // Formation selection
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            RequestSquadFormationChange(SquadFormationType.Offensive);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            RequestSquadFormationChange(SquadFormationType.Defensive);
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            RequestSquadFormationChange(SquadFormationType.CQB);
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            RequestSquadFormationChange(SquadFormationType.DigIn);
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