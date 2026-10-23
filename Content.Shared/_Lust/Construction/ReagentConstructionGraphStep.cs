using Content.Shared.Chemistry.Reagent;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Stacks;
using Robust.Shared.Prototypes;

// Шаг живёт в _Lust, но namespace vanilla, чтобы не править using в vanilla-файлах
#pragma warning disable IDE0130
namespace Content.Shared.Construction.Steps;

/// <summary>
/// Из рук берутся стак (одна единица) и ёмкость, в которой только указанный реагент в нужном количестве.
/// </summary>
[DataDefinition]
public sealed partial class ReagentConstructionGraphStep : ConstructionGraphStep
{
    [DataField(required: true)]
    public ProtoId<StackPrototype> Stack;

    [DataField(required: true)]
    public ProtoId<ReagentPrototype> Reagent;

    [DataField(required: true)]
    public FixedPoint2 Amount;

    public override void DoExamine(ExaminedEvent examinedEvent)
    {
        examinedEvent.PushMarkup(Loc.GetString("construction-insert-reagent", Arguments()));
    }

    public override ConstructionGuideEntry GenerateGuideEntry()
    {
        return new ConstructionGuideEntry
        {
            Localization = "construction-presenter-reagent-step",
            Arguments = Arguments(),
        };
    }

    private (string, object)[] Arguments()
    {
        var prototypes = IoCManager.Resolve<IPrototypeManager>();
        return new (string, object)[] { ("amount", Amount), ("reagent", prototypes.Index(Reagent).LocalizedName) };
    }
}
