using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Optimizer.Logic.Work;
using Optimizer.Logic.Work.AssignmentOptimization;
using Optimizer.Logic.Work.ChairPersonOptimization;

namespace Optimizer.Logic;

public enum OptimizationAlgorithmState
{
    Starting,
    ChairPersonOptimization,
    AssignmentOptimization,
    Done
}

public class OptimizationAlgorithm
{
    private readonly CancellationTokenSource _cts;
    private readonly Task _task;
    private readonly ILoggerFactory _loggerFactory;

    public CancellationToken CancellationToken => _cts.Token;

    public IOptimizerStateDetails<Work.ChairPersonOptimization.OptimizerOutput>? ChairPersonOptimizerStateDetails
    {
        get;
        set;
    }

    public IOptimizerStateDetails<Work.AssignmentOptimization.OptimizerOutput>? AssignmentOptimizerStateDetails
    {
        get;
        set;
    }

    public OptimizationAlgorithmState State { get; private set; } = OptimizationAlgorithmState.Starting;

    private bool CheckIfTaskIsRunning()
    {
        if (_task.Status == TaskStatus.Running)
            return true;

        if (_task.Status == TaskStatus.WaitingToRun)
            return true;

        if (_task.Status == TaskStatus.WaitingForActivation)
            return true;

        if (_task.IsCompleted == false)
            return true;

        return false;
    }

    public void Cancel()
    {
        _cts.Cancel();
    }

    public bool IsWorking => CheckIfTaskIsRunning();

    public Exception? GetThrownException() => _task.Exception;

    public static OptimizationAlgorithm Optimize(Input input, CancellationToken? cancellationToken, ILoggerFactory? loggerFactory = null)
    {
        var (passesValidation, validationMessage) = PassesValidation(input);
        if (!passesValidation)
            throw new($"Validation failed ({validationMessage})");

        var token = cancellationToken ?? CancellationToken.None;
        var factory = loggerFactory ?? new NullLoggerFactory();

        return new OptimizationAlgorithm(input, token, factory);
    }

    private OptimizationAlgorithm(Input input, CancellationToken cancellationToken, ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _task = Task.Factory.StartNew(() => FindSolution(input),
            _cts.Token,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
    }

    private void FindSolution(Input input)
    {
        var focts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
        var firstOptimizer = new ChairPersonOptimizer(input, focts.Token);
        ChairPersonOptimizerStateDetails = firstOptimizer.StateDetails;
        State = OptimizationAlgorithmState.ChairPersonOptimization;
        
        // immediately stop after finding the first solution
        ChairPersonOptimizerStateDetails.OnBetterSolutionFound += (_, _) =>
        {
            focts.Cancel();
        };
        firstOptimizer.Optimize();

        if (firstOptimizer.StateDetails.Result == null)
        {
            State = OptimizationAlgorithmState.Done;
            return;
        }

        var secondOptimizer = new AssignmentOptimizer(input, firstOptimizer.StateDetails.Result, _cts.Token);
        AssignmentOptimizerStateDetails = secondOptimizer.StateDetails;
        State = OptimizationAlgorithmState.AssignmentOptimization;
        secondOptimizer.Optimize();

        State = OptimizationAlgorithmState.Done;
    }

    private static (bool passed, string? message) PassesValidation(Input input)
    {
        // assert that person IDs are continuous, and start at 0
        var peopleIds = input.AvailableChairPersonIds
            .Union(input.DefensesToAssign.Select(d => d.PromoterId))
            .Union(input.DefensesToAssign.Select(d => d.ReviewerId))
            .OrderBy(i => i)
            .ToArray();
        var min = peopleIds.First();
        var max = peopleIds.Last();
        var count = peopleIds.Count();

        if (min != 0)
            return (false, $"min person id != 0 (is: {min})");

        if (max != count - 1)
            return (false, $"max person id != count + 1 (is: {max})");

        // assert that no defense points to same two people
        var samePersonPair = input.DefensesToAssign.FirstOrDefault(d => d.PromoterId == d.ReviewerId);
        if (samePersonPair != null)
            return (false, $"defense promoterId == reviewerId (personId: {samePersonPair.PromoterId})");

        // assert that there are enough slots
        var slotsSum = input.Days.Sum(d => d.Classrooms.Sum(c => c.InputSlots.Length));
        var defenses = input.DefensesToAssign.Sum(d => d.TotalCount);
        if (slotsSum < defenses)
            return (false, $"not enough slots ({slotsSum}) for defenses ({defenses})");

        return (true, null);
    }
}