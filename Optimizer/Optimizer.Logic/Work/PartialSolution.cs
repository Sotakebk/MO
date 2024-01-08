using System.Runtime.InteropServices;

namespace Optimizer.Logic.Work;

public struct PartialSolution
{
    public Day[] Days;
    public Dictionary<(byte supervisorId, byte reviewerId), int> SupervisorAndReviewerIdToAssignmentsLeft;
    public Dictionary<byte, int> ChairPersonAppearanceCount;
    public decimal Score;

    public PartialSolution()
    {
        Days = Array.Empty<Day>();
        SupervisorAndReviewerIdToAssignmentsLeft = new();
        ChairPersonAppearanceCount = new();
        Score = 0;
    }

    public PartialSolution(Input input)
    {
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
        foreach(var chairPerson in input.ChairPersonIds)
        {
            ChairPersonAppearanceCount[(byte)chairPerson] = 0;
        }
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

[StructLayout(LayoutKind.Explicit, Size = 4)]
public struct Assignment
{
    public const int SizeInBytes = 4;

    [FieldOffset(0)] private byte _chairPersonId = 0;
    [FieldOffset(1)] private byte _supervisorId = 0;
    [FieldOffset(2)] private byte _reviewerId = 0;
    [FieldOffset(3)] private byte _valueFlags = 0;

    public byte ChairPersonId => _chairPersonId;
    public byte SupervisorId => _supervisorId;
    public byte ReviewerId => _reviewerId;

    public bool IsAllSet => _valueFlags == 0b11;
    public bool IsChairPersonSet => (_valueFlags & 0b10) != 0;
    public bool IsSupervisorAndReviewerSet => (_valueFlags & 0b01) != 0;

    public Assignment()
    {
    }

    public void SetSupervisorAndReviewer(byte supervisorId, byte reviewerId)
    {
#if DEBUG
        if (IsSupervisorAndReviewerSet)
        {
            throw new Exception("Supervisor and reviewer overwrite was attempted, something is wrong!");
        }
#endif
        _supervisorId = supervisorId;
        _reviewerId = reviewerId;
        _valueFlags |= 0b01;
    }

    public void SetChairPerson(byte chairPersonId)
    {
#if DEBUG
        if (IsChairPersonSet)
        {
            throw new Exception("ChairPerson overwrite was attempted, something is wrong!");
        }
#endif
        _chairPersonId = chairPersonId;
        _valueFlags |= 0b10;
    }

    public void UnsetChairPerson(){
        _chairPersonId = 0;
        _valueFlags &= 0b01;
    }
}