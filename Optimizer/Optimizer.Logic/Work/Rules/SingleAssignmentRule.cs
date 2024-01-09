namespace Optimizer.Logic.Work.Rules;

internal static class SingleAssignmentRule
{
    internal static bool PassesRule(AvailableAction action, PartialSolution solution)
    {
        var day = solution.Days[action.AssignmentId.Day];

        for (var i = 0; i < day.Classrooms.Length; i++)
        {
            var assignment = day.Classrooms[i].Assignments[action.AssignmentId.Assignment];
            if (HasCollision(action, assignment))
                return false;
        }

        return true;
    }

    private static bool HasCollision(AvailableAction action, Assignment assignment)
    {
        var a1 = action.SupervisorId;
        var b1 = action.ChairPersonId;
        var c1 = action.ReviewerId;

        var a2 = assignment.SupervisorId;
        var b2 = assignment.ChairPersonId;
        var c2 = assignment.ReviewerId;

        return a1 == a2 || a1 == b2 || a1 == c2
               || b1 == a2 || b1 == b2 || b1 == c2
               || c1 == a2 || c1 == b2 || c1 == c2;
    }
}