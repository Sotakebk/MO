using Optimizer.Logic.Work.ChairPersonOptimization.Rules;
using Optimizer.Logic.Work.ChairPersonOptimization.Scores;

namespace Optimizer.Logic.Work.ChairPersonOptimization;

internal sealed class ChairPersonOptimizer : BaseOptimizer<OptimizerState, OptimizerAction, OptimizerOutput>
{
    private readonly Input _input;
    private readonly TransformedInput _tInput;

    public ChairPersonOptimizer(Input input, CancellationToken cancellationToken) : base(cancellationToken)
    {
        _input = input;
        _tInput = new TransformedInput(_input);
    }

    internal bool ActionPassesAllRules(OptimizerAction action, OptimizerState state)
    {
        return NoCollisionRule.PassesRule(action, state, _tInput);
    }

    internal static IEnumerable<Position> EnumerableAssignablePositions(OptimizerState state, TransformedInput _tInput)
    {
        for (var d = 0; d < state.Days.Length; d++)
        {
            var day = state.Days[d];
            for (var c = 0; c < day.Classrooms.Length; c++)
            {
                var classroom = day.Classrooms[c];
                for (var b = 0; b < classroom.Blocks.Length; b++)
                {
                    var block = classroom.Blocks[b];
                    if (block.IsAssigned)
                        continue;
                    yield return new Position((byte)d, (byte)c, (byte)b, (byte)_tInput.Days[d].Classrooms[c].Blocks[b].Length);
                }
            }
        }
    }

    protected override OptimizerAction[] CollectAvailableActions(OptimizerState state)
    {
        var positions = EnumerableAssignablePositions(state, _tInput).ToArray();

        var actions = new List<OptimizerAction>();

        Parallel.For(0, positions.Length, index =>
        {
            foreach (var chairPersonId in _input.AvailableChairPersonIds)
            {
                var action = new OptimizerAction()
                {
                    ChairPersonId = chairPersonId,
                    Position = positions[index],
                };

                if (ActionPassesAllRules(action, state))
                {
                    lock (actions)
                        actions.Add(action);
                }
            }
        });

        var value = actions.ToArray();

        return value;
    }

    protected override OptimizerAction[] CollectAvailableActionsMultithreaded(OptimizerState state)
    {
        var positions = EnumerableAssignablePositions(state, _tInput);

        var actions = new List<OptimizerAction>();

        foreach (var position in positions)
        {
            foreach (var chairPersonId in _input.AvailableChairPersonIds)
            {
                var action = new OptimizerAction()
                {
                    ChairPersonId = chairPersonId,
                    Position = position,
                };
                if (ActionPassesAllRules(action, state))
                {
                    actions.Add(action);
                }
            }
        }

        return actions.ToArray();
    }

    protected override OptimizerState CreateStartState()
    {
        return new OptimizerState(_tInput);
    }

    protected override OptimizerOutput CreateOutputFromState(OptimizerState state)
    {
        return new OptimizerOutput(state, _input, _tInput);
    }

    protected override float CalculateScore(OptimizerState state)
    {
        return PreferenceAndEqualDistributionScore.CalculateScore(state, _tInput);
    }

    protected override (int depth, float proportion, float persistence) GetSearchingStrategyForState(OptimizerState state)
    {
        var percentage = Math.Clamp(((1 + state.Depth) / (float)state.MaxDepth), 0, 1);

        // var proportion = Math.Clamp(((1 + state.Depth) / (float)state.MaxDepth), 0, 1);
        // var persistence = proportion;
        // var depth = int.MaxValue;
        // return (depth, proportion, persistence);
        return (0, ExponentialPercentage(percentage), ExponentialPercentage(percentage));
    }

    float ExponentialPercentage(float percentage, float a = 0.01f, float t = 1.0f)
    {
        return MathF.Pow(a * (1.0f / a), percentage * t);
    }
}