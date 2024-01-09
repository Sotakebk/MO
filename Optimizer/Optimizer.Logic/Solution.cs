namespace Optimizer.Logic;

public struct Solution
{
    public float Score { get; set; }
    public SolutionDay[] Days { get; set; }
}

public struct SolutionDay
{
    public int DayId { get; set; }
    public SolutionClassroom[] Classrooms { get; set; }

    public override string ToString()
    {
        return $"Day ID: {DayId} with {Classrooms.Length} classrooms";
    }
}

public struct SolutionClassroom
{
    public int RoomId { get; set; }
    public SolutionAssignment?[] Assignments { get; set; }

    public override string ToString()
    {
        return $"ID:'{RoomId}', [{string.Join(", ", Assignments.Select(a=>$"({a.ToString()})")).Substring(0, 20)}]";
    }
}

public struct SolutionAssignment
{
    public int ChairPersonId { get; set; }
    public int SupervisorId { get; set; }
    public int ReviewerId { get; set; }

    public override string ToString()
    {
        return $"cp: '{ChairPersonId}', s: '{SupervisorId}', r: '{ReviewerId}'";
    }
}