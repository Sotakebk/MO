using Microsoft.Extensions.Logging;

namespace Optimizer.Logic;

public enum OptimizerType
{
    Simple,
    Deep
}

public class Root
{
    private readonly ILoggerFactory _loggerFactory;

    public Root(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    public IOptimizationState Optimize(Input input, CancellationToken? cancellationToken, OptimizerType optimizerType)
    {
        var state = new OptimizationState(cancellationToken);

        ValidateInput(input);

        var optimizer = new Work.Optimizer(_loggerFactory, input, state);

        var task = Task.Factory.StartNew(() => optimizer.Optimize(), state.CancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);

        state.Task = task;

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