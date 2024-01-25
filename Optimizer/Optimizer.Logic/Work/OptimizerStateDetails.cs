namespace Optimizer.Logic.Work;

public interface IOutput
{
    float Score { get; }
}

public interface IOptimizerStateDetails
{
    bool Complete { get; }
    int CurrentDepth { get; }
    float CurrentDepthCompleteness { get; }
    float PercentDomainSeen { get; }
    int MaxDepth { get; }
    int DeadEnds { get; }
    int OperationsDone { get; }
    int Evaluations { get; }
    Task? Task { get; }
    float PartialScore { get; }
    float? ResultScore { get; }
}

public interface IOptimizerStateDetails<TOutput> : IOptimizerStateDetails
    where TOutput : IOutput
{
    public delegate void BetterSolutionFoundEvent(object sender, TOutput newResult);

    public event BetterSolutionFoundEvent OnBetterSolutionFound;

    TOutput? Result { get; }
}

internal class BaseOptimizerStateDetails<TOutput> : IOptimizerStateDetails<TOutput>
    where TOutput : IOutput
{
    public event IOptimizerStateDetails<TOutput>.BetterSolutionFoundEvent? OnBetterSolutionFound;

    public bool Complete { get; set; }
    public TOutput? Result { get; set; }
    public int CurrentDepth { get; set; }
    public float CurrentDepthCompleteness { get; set; }
    public float PercentDomainSeen { get; set; }
    public int MaxDepth { get; set; }
    public int DeadEnds { get; set; }
    public int OperationsDone { get; set; }
    public int Evaluations { get; set; }
    public Task? Task { get; set; }
    public float PartialScore { get; set; }
    public float? ResultScore => Result?.Score;

    public void InvokeBetterSolutionFoundEvent(object sender, TOutput result)
    {
        OnBetterSolutionFound?.Invoke(sender, result);
    }
}
