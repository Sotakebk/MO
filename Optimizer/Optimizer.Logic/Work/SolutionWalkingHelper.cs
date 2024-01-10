namespace Optimizer.Logic.Work;

internal static class SolutionWalkingHelper
{
    public static IEnumerable<AssignmentIndex> EnumerableEmptyAssignments(PartialSolution solution)
    {
        for (var d = 0; d < solution.Days.Length; d++)
        {
            var day = solution.Days[d];
            for (var b = 0; b < day.Classrooms.Length; b++)
            {
                var classroom = day.Classrooms[b];
                for (var s = 0; s < classroom.Assignments.Length; s++)
                {
                    var assignment = classroom.Assignments[s];
                    if (assignment.HasValuesSet())
                        continue;
                    yield return new AssignmentIndex((byte)d, (byte)b, (byte)s);
                }
            }
        }
    }
}