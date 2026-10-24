using Content.Shared.Chemistry.Reagent;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Stacks;
using Robust.Shared.Prototypes;

#pragma warning disable IDE0130
namespace Content.Shared.Construction.Steps;

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
            Icon = IoCManager.Resolve<IPrototypeManager>().Index(Stack).Icon,
        };
    }

    private (string, object)[] Arguments()
    {
        var prototypes = IoCManager.Resolve<IPrototypeManager>();
        return new (string, object)[]
        {
            ("stack", Loc.GetString(prototypes.Index(Stack).Name, ("amount", 1))),
            ("amount", Amount),
            ("reagent", prototypes.Index(Reagent).LocalizedName),
        };
    }
}
