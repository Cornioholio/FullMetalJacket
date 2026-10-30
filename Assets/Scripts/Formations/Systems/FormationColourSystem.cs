
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

/// <summary>
/// Sets marker colours based on their owner's formation membership.
/// </summary>
public partial class FormationMarkerColorSystem : SystemBase
{
    private static readonly float4 UnassignedColour =
        new float4(0.75f, 0.75f, 0.75f, 1f);

    protected override void OnUpdate()
    {
        // Find markers that have an owner and a colour override.
        EntityQuery markerQuery = GetEntityQuery(
            ComponentType.ReadOnly<MarkerOwner>(),
            ComponentType.ReadWrite<URPMaterialPropertyBaseColor>());

        using NativeArray<Entity> markers =
            markerQuery.ToEntityArray(Allocator.Temp);

        foreach (Entity marker in markers)
        {
            MarkerOwner markerOwner =
                EntityManager.GetComponentData<MarkerOwner>(marker);

            Entity owner = markerOwner.value_;

            // Invalid owners or owners without membership use grey.
            if (!EntityManager.Exists(owner) ||
                !EntityManager.HasComponent<FormationMembership>(owner))
            {
                SetMarkerColour(marker, UnassignedColour);
                continue;
            }

            FormationMembership membership =
                EntityManager.GetComponentData<FormationMembership>(owner);

            // Unassigned units use grey.
            if (membership.value_ == Entity.Null)
            {
                SetMarkerColour(marker, UnassignedColour);
                continue;
            }

            // Validate the assigned squad before reading its data.
            if (!EntityManager.Exists(membership.value_) ||
                !EntityManager.HasComponent<Squad>(membership.value_))
            {
                SetMarkerColour(marker, UnassignedColour);
                continue;
            }

            Squad squad =
                EntityManager.GetComponentData<Squad>(membership.value_);

            SetMarkerColour(marker, squad.colour_);
        }
    }

    /// <summary>
    /// Applies a colour to a marker's material override.
    /// </summary>
    private void SetMarkerColour(Entity marker, float4 colour)
    {
        URPMaterialPropertyBaseColor materialColour =
            EntityManager.GetComponentData<URPMaterialPropertyBaseColor>(marker);

        materialColour.Value = colour;

        EntityManager.SetComponentData(marker, materialColour);
    }
}
