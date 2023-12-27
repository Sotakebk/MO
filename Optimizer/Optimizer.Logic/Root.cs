using Microsoft.Extensions.Logging;
using Optimizer.Logic.Work.Score.Heuristics;
using Optimizer.Logic.Work.Score.Rules;

namespace Optimizer.Logic;

public class Root
{
    private LoggerFactory _loggerFactory;

    public Root(LoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    public IOptimizationState Optimize(Input input, CancellationToken? cancellationToken)
    {
        var state = new OptimizationState(cancellationToken);
        var optimizer = new Work.Optimizer(_loggerFactory, input, state, new IRule[] { }, new IHeuristic[] { });

        ValidateInput(input);

        Task.Factory.StartNew(() => optimizer.Optimize(), state.CancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);

        return state;
    }

    public void ValidateInput(Input input)
    {
        if (input.ChairPersonIds.Any(c => c > byte.MaxValue))
            throw new Exception($"ChairPerson ID greater than {byte.MaxValue}");
        if (input.Combinations.Any(c => c.PromoterId > byte.MaxValue || c.ReviewerId > byte.MaxValue))
            throw new Exception($"Promoter or reviewer ID greater than {byte.MaxValue}");
        if (input.Combinations.Any(c => c.TotalCount < 0))
            throw new Exception("TotalCount less than 0");
    }
}