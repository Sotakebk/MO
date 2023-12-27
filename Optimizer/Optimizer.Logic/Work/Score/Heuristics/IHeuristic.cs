namespace Optimizer.Logic.Work.Score.Heuristics;

internal interface IHeuristic
{
    decimal CalculateScore(PartialSolution solution);
}