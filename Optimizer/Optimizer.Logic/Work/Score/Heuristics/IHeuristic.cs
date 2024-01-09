namespace Optimizer.Logic.Work.Score.Heuristics;

public interface IHeuristic
{
    float CalculateScore(PartialSolution solution);
}