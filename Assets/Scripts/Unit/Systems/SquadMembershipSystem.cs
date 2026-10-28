using UnityEngine;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Assertions.Must;

public partial class SquadMembershipSystem : SystemBase 
{
    protected override void OnUpdate()
    {
        // Find soldiers that have requested squad assignment.
        EntityQuery requestQuery = GetEntityQuery(ComponentType.ReadOnly<SquadAssignmentRequest>(), ComponentType.ReadOnly<SquadMembership>());
        using NativeArray<Entity> units = requestQuery.ToEntityArray(Allocator.Temp);

        foreach (Entity unit in units) 
        {
            Debug.Log($"Processing squad assignment for {unit}");
            SquadAssignmentRequest request = EntityManager.GetComponentData<SquadAssignmentRequest>(unit);
            SquadMembership membership = EntityManager.GetComponentData<SquadMembership>(unit);

            Entity newSquad = request.value_;
            Entity oldSquad = membership.value_;

            Debug.Log($"New squad: {newSquad}, old squad: {oldSquad}");
            Debug.Log($"Squad exists: {EntityManager.Exists(newSquad)}");
            Debug.Log($"Has member buffer: {EntityManager.Exists(newSquad) && EntityManager.HasBuffer<SquadMember>(newSquad)}");

            // Validate destination squad
            if (newSquad == Entity.Null ||
                !EntityManager.Exists(newSquad) ||
                !EntityManager.HasBuffer<SquadMember>(newSquad)) 
            {
                EntityManager.RemoveComponent<SquadAssignmentRequest>(unit);
                continue;
            }

            // Remove the unit from old squad
            if (oldSquad != Entity.Null &&
                EntityManager.Exists(oldSquad) &&
                EntityManager.HasBuffer<SquadMember>(oldSquad))
            {
                DynamicBuffer<SquadMember> oldMembers = EntityManager.GetBuffer<SquadMember>(oldSquad);

                for (int i = oldMembers.Length - 1; i >= 0; i--)
                {
                    if (oldMembers[i].value_ == unit) 
                    {
                        oldMembers.RemoveAt(i);
                    }
                }
            }

            // Add unit to the new squad
            DynamicBuffer<SquadMember> newMembers = EntityManager.GetBuffer<SquadMember>(newSquad);

            Debug.Log($"New squad buffer length before assignment: {newMembers.Length}");

            bool alreadyMember = false;

            for (int i = 0; i < newMembers.Length; i++)
            {
                if (newMembers[i].value_ == unit) 
                {
                    alreadyMember = true;
                    break;
                }
            }

            if (!alreadyMember) 
            {
                newMembers.Add(new SquadMember
                {
                    value_ = unit
                });
            }

            membership.value_ = newSquad;

            EntityManager.SetComponentData(unit, membership);
            EntityManager.RemoveComponent<SquadAssignmentRequest>(unit);

            Debug.Log($"Membership after assignment: {EntityManager.GetComponentData<SquadMembership>(unit).value_}");
            Debug.Log($"New squad buffer length after assignment: {EntityManager.GetBuffer<SquadMember>(newSquad).Length}");
        }

    }
}