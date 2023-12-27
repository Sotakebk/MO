namespace Optimizer.Logic;

public interface IOptimizationState
{
    bool IsWorking { get; }
    Solution? Result { get; }
    CancellationToken CancellationToken { get; }
    int CurrentDepth { get; }
    int OperationsDone { get; }
}