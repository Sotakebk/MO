namespace Optimizer.Logic.Work.Score.Rules;

class SingleAssignmentRule : IRule
{
    public bool PassesRule(AvailableAction action, PartialSolution solution)
    {
        if (!VerifySingleAssignmentInBlock(action, solution))
            return false;

        if (!VerifySingleAssignmentInTime(action, solution))
            return false;

        return true;
    }

    private static bool VerifySingleAssignmentInBlock(AvailableAction action, PartialSolution solution)
    {
        var assignment = solution
            .Days[action.AssignmentId.Day]
            .Blocks[action.AssignmentId.Block]
            .Assignments[action.AssignmentId.Assignment];

        if (action.Type == AvailableActionType.SetChairPerson && assignment.IsChairPersonSet)
        {
            if (assignment.ReviewerId == action.ChairPersonId) // try to assign chairperson which is reviewer
                return false;

            if (assignment.SupervisorId == action.ChairPersonId) // try to assign chairperson which is supervisor
                return false;
        }
        else if (action.Type == AvailableActionType.SetSupervisorAndReviewer && assignment.IsSupervisorAndReviewerSet)
        {
            if (assignment.ChairPersonId == action.ReviewerId) // try to assign ReviewerId which is chairperson
                return false;

            if (assignment.ChairPersonId == action.SupervisorId) // try to assign SupervisorId which is chairperson
                return false;
        }

        return true;
    }

    private static bool VerifySingleAssignmentInTime(AvailableAction action, PartialSolution solution)
    {
        foreach (var block in solution.Days[action.AssignmentId.Day].Blocks)
        {
            if (block.Assignments.Length < action.AssignmentId.Assignment - block.Offset)
                continue;

            var assignment = block.Assignments[action.AssignmentId.Assignment - block.Offset];

            if (action.Type == AvailableActionType.SetChairPerson)
            {
                if (assignment.IsSupervisorAndReviewerSet)
                {
                    if (assignment.ReviewerId == action.ChairPersonId) // try to assign chairperson which is reviewer in other block in same time
                        return false;

                    if (assignment.SupervisorId == action.ChairPersonId) // try to assign chairperson which is supervisor in other block in same time
                        return false;
                }

                if (assignment.IsChairPersonSet)
                {
                    if (assignment.ChairPersonId == action.ChairPersonId) // try to assign chairperson which is chairperson in other block in same time
                        return false;
                }
            }
            else if (action.Type == AvailableActionType.SetSupervisorAndReviewer)
            {
                if (assignment.IsSupervisorAndReviewerSet)
                {
                    if (assignment.ReviewerId == action.ReviewerId) // try to assign reviewer which is reviewer in other block in same time
                        return false;

                    if (assignment.SupervisorId == action.SupervisorId) // try to assign supervisor which is supervisor in other block in same time
                        return false;
                }

                if (assignment.IsChairPersonSet)
                {
                    if (assignment.ChairPersonId == action.ReviewerId) // try to assign reviewer which is chairperson in other block in same time
                        return false;

                    if (assignment.ChairPersonId == action.SupervisorId) // try to assign supervisor which is chairperson in other block in same time
                        return false;
                }
            }
        }

        return false;
    }
}