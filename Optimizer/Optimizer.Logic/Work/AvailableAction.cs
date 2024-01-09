using System.Runtime.InteropServices;
using Optimizer.Logic.Extensions;

namespace Optimizer.Logic.Work;

[StructLayout(LayoutKind.Explicit, Size = AssignmentIndex.StructureSize + 3 * sizeof(byte) + sizeof(float))]
internal struct AvailableAction
{
    [FieldOffset(0)]
    public AssignmentIndex AssignmentId;

    [FieldOffset(AssignmentIndex.StructureSize)]
    public byte ChairPersonId;

    [FieldOffset(AssignmentIndex.StructureSize + 1)]
    public byte SupervisorId;

    [FieldOffset(AssignmentIndex.StructureSize + 2)]
    public byte ReviewerId;

    [FieldOffset(AssignmentIndex.StructureSize + 3)]
    public float Score;
    
    public AvailableAction(byte supervisorId, byte reviewerId, byte chairPersonId, AssignmentIndex assignmentId)
    {
        ChairPersonId = chairPersonId;
        SupervisorId = supervisorId;
        ReviewerId = reviewerId;
        AssignmentId = assignmentId;
        Score = 0;
    }

    public readonly void Apply(ref PartialSolution solution)
    {
        var assignments = solution.Days[AssignmentId.Day].Classrooms[AssignmentId.Classroom].Assignments;

        assignments[AssignmentId.Assignment].SetAssignment(SupervisorId, ReviewerId, ChairPersonId);

        solution.ChairPersonAppearanceCount.AddOrUpdate(ChairPersonId, 1, c => c + 1);

        var count = solution.SupervisorAndReviewerIdToAssignmentsLeft[(SupervisorId, ReviewerId)];
        solution.SupervisorAndReviewerIdToAssignmentsLeft[(SupervisorId, ReviewerId)] = count - 1;

        solution.Score = Score;
        solution.CurrentDepth++;
    }

    public readonly override string ToString()
    {
        return $"Action: score:{Score}";
    }
}