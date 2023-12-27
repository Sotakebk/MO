namespace Optimizer.Logic.Work.Score.Rules;

internal interface IRule
{
    /// <summary>
    /// Should NEVER mutate solution
    /// </summary>
    bool PassesRule(AvailableAction action, PartialSolution solution);

    bool PassesRule(PartialSolution solution);
}