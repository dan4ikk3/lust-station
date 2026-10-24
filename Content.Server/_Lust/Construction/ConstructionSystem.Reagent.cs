using System.Linq;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Construction.Steps;
using Content.Shared.FixedPoint;
using Content.Shared.Stacks;

namespace Content.Server.Construction;

public sealed partial class ConstructionSystem
{
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;

    /// <summary>
    /// Ищет в руках стак и ёмкость, в которой только нужный реагент в нужном количестве.
    /// </summary>
    private bool TryFindReagentCraft(
        EntityUid user,
        ReagentConstructionGraphStep step,
        out Entity<StackComponent> stack,
        out Entity<SolutionComponent> solution)
    {
        var held = _handsSystem.EnumerateHeld(user).ToArray();
        foreach (var item in held)
        {
            if (!TryComp<StackComponent>(item, out var stackComp) || stackComp.StackTypeId != step.Stack)
                continue;

            foreach (var container in held)
            {
                if (!HasComp<DrainableSolutionComponent>(container)
                    || !_solutionContainer.TryGetDrainableSolution(container, out var soln, out var contents))
                    continue;

                var quantity = contents.GetTotalPrototypeQuantity(step.Reagent);
                if (quantity < step.Amount || contents.Volume > quantity)
                    continue;

                stack = (item, stackComp);
                solution = soln.Value;
                return true;
            }
        }

        stack = default;
        solution = default;
        return false;
    }

    /// <summary>
    /// Списывает ингредиенты по reagent-шагам ребра. Сначала проверяются все шаги вместе, потом списывается всё сразу.
    /// </summary>
    private bool TryConsumeReagentCraft(EntityUid user, IEnumerable<ConstructionGraphStep> steps)
    {
        var found = new List<(Entity<StackComponent> Stack, Entity<SolutionComponent> Solution, ReagentConstructionGraphStep Step)>();
        foreach (var step in steps.OfType<ReagentConstructionGraphStep>())
        {
            if (!TryFindReagentCraft(user, step, out var stack, out var solution))
                return false;

            found.Add((stack, solution, step));
        }

        // Одинаковые шаги не должны делить одну порцию стака или реагента на двоих
        foreach (var (stack, solution, step) in found)
        {
            if (found.Count(f => f.Stack.Owner == stack.Owner) > stack.Comp.Count)
                return false;

            var need = FixedPoint2.Zero;
            foreach (var other in found.Where(f => f.Solution.Owner == solution.Owner && f.Step.Reagent == step.Reagent))
            {
                need += other.Step.Amount;
            }

            if (solution.Comp.Solution.GetTotalPrototypeQuantity(step.Reagent) < need)
                return false;
        }

        foreach (var (stack, solution, step) in found)
        {
            _solutionContainer.RemoveReagent(solution, step.Reagent, step.Amount);
            _stackSystem.ReduceCount((stack.Owner, stack.Comp), 1);
        }

        return true;
    }
}
