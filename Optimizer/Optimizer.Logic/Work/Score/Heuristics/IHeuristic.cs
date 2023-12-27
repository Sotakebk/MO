namespace Optimizer.Logic.Work.Score.Heuristics;

internal interface IHeuristic
{
    decimal CalculateScore(AvailableAction action, PartialSolution solution);
    decimal CalculateScore(PartialSolution solution);
}