namespace Optimizer.Logic;

internal class OptimizationState : IOptimizationState
{
    private readonly CancellationTokenSource _cts;

    private bool _isWorking;

    private bool CheckIfTaskIsRunning(){
        if(Task == null)
            return false;
        
        return Task.IsCompleted == false &&
            (
                Task.Status == TaskStatus.Running
                || Task.Status != TaskStatus.WaitingToRun
                || Task.Status != TaskStatus.WaitingForActivation
            );
    }

    public bool IsWorking
    {
        get => _isWorking && CheckIfTaskIsRunning();
        set => _isWorking = value;
    }

    public Solution? Result { get; set; }

    public CancellationToken CancellationToken { get; set; }
    public int CurrentDepth { get; set; }
    public int OperationsDone { get; set; }
    public int DeadEnds { get; set; }
    public Task? Task { get; set; }

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