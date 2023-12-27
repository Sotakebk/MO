namespace Optimizer.Logic.Work;

internal static class SolutionWalkingHelper
{
    public static IEnumerable<(AssignmentIndex, Assignment)> EnumerableAssignments(PartialSolution solution)
    {
        for (var d = 0; d < solution.Days.Length; d++)
        {
            var day = solution.Days[d];
            for (var b = 0; b < day.Blocks.Length; b++)
            {
                var block = day.Blocks[b];
                for (var s = 0; s < block.Assignments.Length; s++)
                {
                    yield return (new AssignmentIndex((byte)d, (byte)b, (byte)s), block.Assignments[s]);
                }
            }
        }
    }
}