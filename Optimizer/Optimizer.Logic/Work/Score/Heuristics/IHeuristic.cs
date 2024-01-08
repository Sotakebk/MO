namespace Optimizer.Logic.Work.Score.Heuristics;

public interface IHeuristic
{
    decimal CalculateScore(PartialSolution solution);
}