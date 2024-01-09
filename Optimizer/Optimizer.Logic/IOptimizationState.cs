namespace Optimizer.Logic;

public interface IOptimizationState
{
    bool IsWorking { get; }
    Solution? Result { get; }
    CancellationToken CancellationToken { get; }
    int CurrentDepth { get; }
    int DeadEnds { get; }
    int OperationsDone { get; }
    Task? Task { get; }
}