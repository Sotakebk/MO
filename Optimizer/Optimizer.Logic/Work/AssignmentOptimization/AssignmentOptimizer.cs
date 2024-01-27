using Optimizer.Logic.Work.AssignmentOptimization.Rules;
using Optimizer.Logic.Work.AssignmentOptimization.Scores;

namespace Optimizer.Logic.Work.AssignmentOptimization;

internal sealed class AssignmentOptimizer : BaseOptimizer<OptimizerState, OptimizerAction, OptimizerOutput>
{
    private readonly TransformedInput _tInput;
    private readonly Input _input;

    public AssignmentOptimizer(Input input, ChairPersonOptimization.OptimizerOutput _chairPersonOptimizerOutput, CancellationToken cancellationToken) : base(cancellationToken)
    {
        _input = input;
        _tInput = new TransformedInput(input, _chairPersonOptimizerOutput);
    }

    protected override float CalculateScore(OptimizerState state)
    {
        return GeneralPeopleScore.CalculateScore(state, _tInput);
    }

    private static IEnumerable<Position> EnumerableAssignablePositions(OptimizerState solution)
    {
        for (var d = 0; d < solution.Days.Length; d++)
        {
            var day = solution.Days[d];
            for (var b = 0; b < day.Classrooms.Length; b++)
            {
                var classroom = day.Classrooms[b];
                for (var s = 0; s < classroom.Slots.Length; s++)
                {
                    var assignment = classroom.Slots[s];
                    if (assignment.HasValuesSet())
                        continue;
                    yield return new Position((byte)d, (byte)b, (byte)s);
                }
            }
        }
    }

    internal bool ActionPassesAllRules(OptimizerAction action, OptimizerState state)
    {
        return SingleAssignmentRule.PassesRule(action, state, _tInput);
    }

    protected override OptimizerAction[] CollectAvailableActions(OptimizerState state)
    {
        var assignments = EnumerableAssignablePositions(state).ToArray();
        var actions = new List<OptimizerAction>();

        foreach (var assignmentIndex in assignments)
        {
            foreach (var keyValuePair in state.PairsToAssignLeft)
            {
                if (keyValuePair.Value <= 0)
                    continue;

                var action = new OptimizerAction(
                    keyValuePair.Key.a,
                    keyValuePair.Key.b,
                    assignmentIndex);

                if (ActionPassesAllRules(action, state))
                    actions.Add(action);
            }
        }

        return actions.ToArray();
    }

    protected override OptimizerAction[] CollectAvailableActionsMultithreaded(OptimizerState state)
    {
        var assignments = EnumerableAssignablePositions(state).ToArray();
        var actions = new List<OptimizerAction>();

        Parallel.For(0, assignments.Length, index =>
        {
            var assignmentIndex = assignments[index];

            foreach (var keyValuePair in state.PairsToAssignLeft)
            {
                if (keyValuePair.Value <= 0)
                    continue;

                var action = new OptimizerAction(
                    keyValuePair.Key.a,
                    keyValuePair.Key.b,
                    assignmentIndex);

                if (ActionPassesAllRules(action, state))
                    lock (actions)
                        actions.Add(action);
            }
        });

        return actions.ToArray();
    }

    protected override OptimizerOutput CreateOutputFromState(OptimizerState state)
    {
        return new OptimizerOutput(state, _tInput);
    }

    protected override OptimizerState CreateStartState()
    {
        return new OptimizerState(_input, _tInput);
    }


    protected override (int depth, float proportion, float persistence) GetSearchingStrategyForState(OptimizerState state)
    {
        /*
        var temp = state.Depth / (float)state.MaxDepth;
        temp = temp * temp * temp * temp * temp * temp * temp;
        var depth = Math.Min((int)(solution.MaxDepth * temp), solution.MaxDepth - solution.CurrentDepth);
        var persistence = solution.CurrentDepth / (float)solution.MaxDepth;

        // x^5
        //persistence = persistence * persistence * persistence * persistence * persistence;
        //var proportion = (1 + solution.CurrentDepth) / (float)solution.MaxDepth;
        // x^9
        //proportion = proportion * proportion * proportion * proportion * proportion * proportion * proportion * proportion * proportion;

        /*
        var tempProportionForPrinting = proportion;
        _logger.LogDebug($"Evaluating {actions.Length} actions");
        for (int i = 0; i <= depth; i++)
        {
            _logger.LogDebug($"At child level {i} evaluating actions with {(100*tempProportionForPrinting):F4}% of children");
            tempProportionForPrinting *= persistence;
        }*/

        /*
        var depth = Math.Max(0, solution.CurrentDepth - solution.MaxDepth + 15);
        var proportion = 0.01f;
        var persistence = 1;
        */

        /*
         var temp = state.Depth / (float)state.MaxDepth;
         temp = temp * temp * temp * temp * temp * temp * temp;
         var depth = Math.Min((int)(solution.MaxDepth * temp), solution.MaxDepth - solution.CurrentDepth);
         var persistence = solution.CurrentDepth / (float)solution.MaxDepth;

         // x^5
         //persistence = persistence * persistence * persistence * persistence * persistence;
         //var proportion = (1 + solution.CurrentDepth) / (float)solution.MaxDepth;
         // x^9
         //proportion = proportion * proportion * proportion * proportion * proportion * proportion * proportion * proportion * proportion;

         /*
         var tempProportionForPrinting = proportion;
         _logger.LogDebug($"Evaluating {actions.Length} actions");
         for (int i = 0; i <= depth; i++)
         {
             _logger.LogDebug($"At child level {i} evaluating actions with {(100*tempProportionForPrinting):F4}% of children");
             tempProportionForPrinting *= persistence;
         }*/


        var percentage = state.Depth + 1.0f / state.MaxDepth;
        var exp = ExponentialPercentage(percentage, 0.00001f);
        var depth = (int)(1.0f * state.MaxDepth * exp);
        var proportion = exp;
        var persistence = exp;
        return (depth, proportion, persistence);
    }

    float ExponentialPercentage(float percentage, float a = 0.01f)
    {
        return a * MathF.Pow(a * (1.0f / a), Math.Clamp(percentage, 0f, 1f));
    }
}