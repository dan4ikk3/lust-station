using System.Diagnostics.CodeAnalysis;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Stacks;

namespace Content.Shared._Lust.Chemistry;

/// <summary>
/// ЛКМ по предмету запускает крафт, по завершении состав из
/// ёмкости перепроверяется, реагент списывается, а предмет заменяется на результат рецепта.
/// </summary>
public sealed partial class ReagentCraftSystem : EntitySystem
{
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private SharedStackSystem _stacks = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReagentCraftComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<ReagentCraftComponent, ReagentCraftDoAfterEvent>(OnDoAfter);
    }

    private void OnInteractUsing(Entity<ReagentCraftComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !HasComp<DrainableSolutionComponent>(args.Used) || !CanCraft(ent, args.Used, args.User, out var recipe, out _))
            return;

        args.Handled = _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, recipe.Time, new ReagentCraftDoAfterEvent(), ent, target: ent, used: args.Used)
        {
            NeedHand = true,
            BreakOnMove = true,
            BreakOnHandChange = true,
            DuplicateCondition = DuplicateConditions.SameTarget | DuplicateConditions.SameEvent,
        });
    }

    private void OnDoAfter(Entity<ReagentCraftComponent> ent, ref ReagentCraftDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Used is not { } container)
            return;

        args.Handled = true;

        // Состав ёмкости проверяем заново
        if (CanCraft(ent, container, args.User, out var recipe, out var solution))
            Craft(ent, recipe, solution, args.User);
    }

    /// <summary>
    /// Ищет лучший подходящий рецепт для раствора в ёмкости.
    /// </summary>
    private bool CanCraft(
        Entity<ReagentCraftComponent> ent,
        EntityUid container,
        EntityUid user,
        [NotNullWhen(true)] out ReagentCraftRecipe? recipe,
        out Entity<SolutionComponent> solution)
    {
        recipe = null;
        solution = default;

        if (!_solutionContainer.TryGetDrainableSolution(container, out var soln, out var contents))
            return Fail("reagent-craft-not-enough-reagent");

        solution = soln.Value;

        var best = FixedPoint2.Zero;
        foreach (var candidate in ent.Comp.Recipes)
        {
            var quantity = contents.GetTotalPrototypeQuantity(candidate.Reagent);
            if (quantity < candidate.MinQuantity || quantity <= best)
                continue;

            best = quantity;
            recipe = candidate;
        }

        if (recipe == null)
            return Fail("reagent-craft-not-enough-reagent");

        // реагент должен быть всем раствором, а не частью
        if (contents.Volume - best > FixedPoint2.Zero)
        {
            recipe = null;
            return Fail("reagent-craft-contaminated");
        }

        return true;

        bool Fail(string locId)
        {
            _popup.PopupClient(Loc.GetString(locId, ("item", ent.Owner)), ent, user);
            return false;
        }
    }

    /// <summary>
    /// Списывает реагент из ёмкости и спавнит результат. Из стака тратит одну единицу.
    /// </summary>
    private void Craft(Entity<ReagentCraftComponent> ent, ReagentCraftRecipe recipe, Entity<SolutionComponent> solution, EntityUid user)
    {
        _solutionContainer.RemoveReagent(solution, recipe.Reagent, recipe.MinQuantity);

        var result = PredictedSpawnNextToOrDrop(recipe.Result, ent.Owner);
        _popup.PopupClient(Loc.GetString("reagent-craft-success", ("item", result)), result, user);

        if (TryComp<StackComponent>(ent.Owner, out var stack))
            _stacks.ReduceCount((ent.Owner, stack), 1);
        else
            PredictedQueueDel(ent.Owner);
    }
}
