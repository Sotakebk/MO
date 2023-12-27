namespace Optimizer.Logic;

internal class OptimizationState : IOptimizationState
{
    private readonly CancellationTokenSource _cts;

    public bool IsWorking { get; set; }

    public Solution? Result { get; set; }

    public CancellationToken CancellationToken { get; set; }
    public int CurrentDepth { get; set; }
    public int OperationsDone { get; set; }

    public OptimizationState(CancellationToken? cancellationToken)
    {
        _cts = cancellationToken != null
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken.Value)
            : new CancellationTokenSource();
    }

    public void Cancel()
    {
        _cts.Cancel();
    }
}