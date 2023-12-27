namespace Optimizer.Logic.Work;

public enum AvailableActionType : byte
{
    SetChairPerson,
    SetSupervisorAndReviewer
}

internal struct AvailableAction
{
    public AvailableActionType Type;
    public AssignmentIndex AssignmentId;
    public short Data;
    public decimal Score;

    public byte ChairPersonId => (byte)Data;
    public byte SupervisorId => (byte)(Data >> 8);
    public byte ReviewerId => (byte)(Data & 0xFF);

    public AvailableAction(int chairPersonId, AssignmentIndex assignmentId)
    {
        Type = AvailableActionType.SetChairPerson;
        Data = (short)chairPersonId;
        AssignmentId = assignmentId;
        Score = 0;
    }

    public AvailableAction(int supervisorId, int reviewerId, AssignmentIndex assignmentId)
    {
        Type = AvailableActionType.SetSupervisorAndReviewer;
        Data = (short)((reviewerId & 0xFF) | ((supervisorId & 0xFF) << 8));
        AssignmentId = assignmentId;
        Score = 0;
    }

    public void Apply(PartialSolution solution)
    {
        if (Type == AvailableActionType.SetChairPerson)
        {
            solution.Days[AssignmentId.Day].Blocks[AssignmentId.Block].Assignments[AssignmentId.Assignment]
                .SetChairPerson(ChairPersonId);
        }
        else
        {
            var count = solution.SupervisorAndReviewerIdToAssignmentsLeft[(SupervisorId, ReviewerId)];
            solution.SupervisorAndReviewerIdToAssignmentsLeft[(SupervisorId, ReviewerId)] = count - 1;
            solution.Days[AssignmentId.Day].Blocks[AssignmentId.Block].Assignments[AssignmentId.Assignment]
                .SetSupervisorAndReviewer(SupervisorId, ReviewerId);
        }
    }
}