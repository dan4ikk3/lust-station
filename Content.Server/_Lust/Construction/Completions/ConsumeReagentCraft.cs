using Content.Shared._Lust.Construction;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Construction;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server._Lust.Construction.Completions;

/// <summary>
/// Списывает реагент из ёмкости в руках. Если состав убрали во время крафта, готовый предмет не выдаётся.
/// </summary>
[UsedImplicitly]
[DataDefinition]
public sealed partial class ConsumeReagentCraft : IGraphAction
{
    [DataField(required: true)]
    public ProtoId<ReagentPrototype> Reagent;

    [DataField(required: true)]
    public FixedPoint2 Amount;

    public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
    {
        if (userUid is not { } user)
            return;

        if (ReagentCraftCondition.TryFindContainer(user, Reagent, Amount, out var solution))
        {
            // В ёмкости только этот реагент, поэтому забираем порцию целиком, независимо от данных реагента
            entityManager.System<SharedSolutionContainerSystem>().SplitSolution(solution, Amount);
            return;
        }

        entityManager.System<SharedPopupSystem>().PopupEntity(Loc.GetString("construction-system-construct-no-materials"), user, user);
        entityManager.QueueDeleteEntity(uid);
    }
}
