namespace Optimizer.Logic;

public struct Solution
{
    public decimal Score { get; set; }
    public SolutionDay[] Days { get; set; }
}

public struct SolutionDay
{
    public int DayId { get; set; }
    public SolutionBlock[] VacantBlocks { get; set; }
}

public struct SolutionBlock
{
    public int RoomId { get; set; }
    public int Offset { get; set; }
    public SolutionAssignment[] Assignments { get; set; }
}

public struct SolutionAssignment
{
    public int ChairPersonId { get; set; }
    public int SupervisorId { get; set; }
    public int ReviewerId { get; set; }
}