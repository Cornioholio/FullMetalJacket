using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

public partial class MarkerColourSystem : SystemBase 
{
    private static readonly float4 DefaultColour = new float4(0.75f, 0.75f, 0.75f, 1f);

    private static readonly float4 SelectedColour = new float4(1f, 0.8f, 0.1f, 1f);

    protected override void OnUpdate()
    {
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

            if (!EntityManager.Exists(owner))
                continue;

            float4 colour = GetMarkerColour(
                owner,
                markerOwner.type_);

            SetMarkerColour(marker, colour);
        }
    }

    /// <summary>
    /// Determines the appropriate colour for a marker.
    /// </summary>
    private float4 GetMarkerColour(Entity owner, MarkerType markerType)
    {
        switch (markerType)
        {
            case MarkerType.DirectionSelection:
                return GetSelectionColour(owner);

            case MarkerType.Squad:
                return GetSquadColour(owner);

            default:
                return DefaultColour;
        }
    }

    /// <summary>
    /// Gets the colour based on the owner's selection state.
    /// </summary>
    private float4 GetSelectionColour(Entity owner)
    {
        return EntityManager.HasComponent<Selected>(owner)
            ? SelectedColour
            : DefaultColour;
    }

    /// <summary>
    /// Gets the colour based on the owner's squad membership.
    /// </summary>
    private float4 GetSquadColour(Entity owner)
    {
        if (!EntityManager.HasComponent<FormationMembership>(owner))
            return DefaultColour;

        FormationMembership membership =
            EntityManager.GetComponentData<FormationMembership>(owner);

        if (membership.value_ == Entity.Null ||
            !EntityManager.Exists(membership.value_) ||
            !EntityManager.HasComponent<Squad>(membership.value_))
        {
            return DefaultColour;
        }

        Squad squad =
            EntityManager.GetComponentData<Squad>(membership.value_);

        return squad.colour_;
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