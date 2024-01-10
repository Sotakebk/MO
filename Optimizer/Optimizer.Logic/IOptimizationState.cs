namespace Optimizer.Logic;

public interface IOptimizationState
{
    bool IsWorking { get; }
    Solution? Result { get; }
    CancellationToken CancellationToken { get; }
    int CurrentDepth { get; }
    float CurrentDepthCompleteness { get; }
    float PercentDomainSeen { get; }
    int MaxDepth { get; }
    int DeadEnds { get; }
    int OperationsDone { get; }
    public int Evaluations { get; }
    Task? Task { get; }
    public float PartialScore { get; }
}