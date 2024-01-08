namespace Optimizer.Logic.Work;

internal static class SolutionWalkingHelper
{
    public static IEnumerable<(AssignmentIndex, Assignment)> EnumerableAssignments(PartialSolution solution)
    {
        for (var d = 0; d < solution.Days.Length; d++)
        {
            var day = solution.Days[d];
            for (var b = 0; b < day.Classrooms.Length; b++)
            {
                var classroom = day.Classrooms[b];
                for (var s = 0; s < classroom.Assignments.Length; s++)
                {
                    yield return (new AssignmentIndex((byte)d, (byte)b, (byte)s), classroom.Assignments[s]);
                }
            }
        }
    }
}