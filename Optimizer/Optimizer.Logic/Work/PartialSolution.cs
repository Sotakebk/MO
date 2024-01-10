using System.Runtime.InteropServices;

namespace Optimizer.Logic.Work;

public struct PartialSolution
{
    public Day[] Days;
    public Dictionary<(byte supervisorId, byte reviewerId), int> SupervisorAndReviewerIdToAssignmentsLeft;
    public Dictionary<byte, int> ChairPersonAppearanceCount;
    public IReadOnlyList<byte> PeopleIds;
    public int CurrentDepth = 0;
    public int MaxDepth = 0;
    public float Score = 0;

    public PartialSolution()
    {
        Days = Array.Empty<Day>();
        SupervisorAndReviewerIdToAssignmentsLeft = new();
        ChairPersonAppearanceCount = new();
        PeopleIds = Array.Empty<byte>();
    }

    public PartialSolution(Input input)
    {
        PeopleIds = Array.Empty<byte>();
        Days = new Day[input.Days.Length];
        for (var i = 0; i < input.Days.Length; i++)
        {
            Days[i] = new Day(input.Days[i]);
        }

        SupervisorAndReviewerIdToAssignmentsLeft = new Dictionary<(byte, byte), int>();

        foreach (var pair in input.Combinations)
        {
            SupervisorAndReviewerIdToAssignmentsLeft[((byte)pair.PromoterId, (byte)pair.ReviewerId)] = pair.TotalCount;
        }
        ChairPersonAppearanceCount = new();
        foreach (var chairPerson in input.ChairPersonIds)
        {
            ChairPersonAppearanceCount[(byte)chairPerson] = 0;
        }

        MaxDepth = input.Combinations.Select(c => c.TotalCount).Sum();
        PeopleIds = input.Combinations.Select(c => c.ReviewerId)
            .Union(input.Combinations.Select(c => c.PromoterId))
            .Union(input.ChairPersonIds.Select(c=>c))
            .OrderBy(i=>i)
            .Select(i=>(byte)i)
            .ToArray();
        Score = 0;
    }

    public readonly PartialSolution CreateDeepCopy()
    {
        var ps = new PartialSolution();
        ps.Days = new Day[Days.Length];
        for (var i = 0; i < Days.Length; i++)
        {
            ps.Days[i] = Days[i].CreateDeepCopy();
        }

        ps.SupervisorAndReviewerIdToAssignmentsLeft = new Dictionary<(byte supervisorId, byte reviewerId), int>(SupervisorAndReviewerIdToAssignmentsLeft);
        ps.ChairPersonAppearanceCount = new Dictionary<byte, int>(ChairPersonAppearanceCount);
        ps.MaxDepth = MaxDepth;
        ps.CurrentDepth = CurrentDepth;
        ps.Score = Score;
        ps.PeopleIds = PeopleIds;
        return ps;
    }
}

public struct Day
{
    public int DayId;
    public Classroom[] Classrooms;
    public int SlotCount;

    public Day(InputDay inputDay)
    {
        DayId = inputDay.Id;
        SlotCount = inputDay.SlotCount;
        Classrooms = new Classroom[inputDay.Classrooms.Length];
        for (var i = 0; i < Classrooms.Length; i++)
        {
            Classrooms[i] = new Classroom(inputDay.Classrooms[i], SlotCount);
        }
    }

    public readonly Day CreateDeepCopy()
    {
        var d = new Day();
        d.DayId = DayId;
        d.SlotCount = SlotCount;
        var b = Classrooms;
        d.Classrooms = new Classroom[b.Length];
        for (var i = 0; i < Classrooms.Length; i++)
        {
            d.Classrooms[i] = b[i].CreateDeepCopy();

        }
        return d;
    }
}

public struct Classroom
{
    public int RoomId;
    public Assignment[] Assignments;

    public Classroom(InputClassroom inputClassroom, int slotCount)
    {
        RoomId = inputClassroom.RoomId;
        Assignments = new Assignment[slotCount];
    }

    public readonly Classroom CreateDeepCopy()
    {
        var b = new Classroom();
        b.RoomId = RoomId;
        b.Assignments = new Assignment[Assignments.Length];
        Array.Copy(Assignments, b.Assignments, Assignments.Length);
        return b;
    }
}

[StructLayout(LayoutKind.Explicit, Size = StructureSize)]
public struct Assignment
{
    public const int StructureSize = sizeof(byte)*4;

    [FieldOffset(0)] private byte _chairPersonId = 0;
    [FieldOffset(1)] private byte _supervisorId = 0;
    [FieldOffset(2)] private byte _reviewerId = 0;
    [FieldOffset(3)] private byte _flag = 0;

    public readonly byte ChairPersonId => _chairPersonId;
    public readonly byte SupervisorId => _supervisorId;
    public readonly byte ReviewerId => _reviewerId;

    public Assignment()
    {
    }

    public void SetAssignment(byte supervisorId, byte reviewerId, byte chairPersonId)
    {
#if DEBUG
        if (_flag != 0)
        {
            throw new Exception("Assignment overwrite was attempted, something is wrong!");
        }
#endif
        _supervisorId = supervisorId;
        _reviewerId = reviewerId;
        _chairPersonId = chairPersonId;
        _flag = 1;
    }

    public readonly bool HasValuesSet() => _flag != 0;
}