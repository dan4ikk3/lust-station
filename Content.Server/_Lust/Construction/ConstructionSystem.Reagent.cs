using System.Linq;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Construction.Steps;
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
    /// Списывает ингредиенты по reagent-шагам ребра, состав в руках проверяется заново после DoAfter.
    /// </summary>
    private bool TryConsumeReagentCraft(EntityUid user, IEnumerable<ConstructionGraphStep> steps)
    {
        foreach (var step in steps)
        {
            if (step is not ReagentConstructionGraphStep reagentStep)
                continue;

            if (!TryFindReagentCraft(user, reagentStep, out var stack, out var solution))
                return false;

            _solutionContainer.RemoveReagent(solution, reagentStep.Reagent, reagentStep.Amount);
            _stackSystem.ReduceCount((stack.Owner, stack.Comp), 1);
        }

        return true;
    }
}
