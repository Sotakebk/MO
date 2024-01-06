using System.Runtime.InteropServices;

namespace Optimizer.Logic.Work;

internal struct PartialSolution
{
    public Day[] Days;
    public Dictionary<(byte supervisorId, byte reviewerId), int> SupervisorAndReviewerIdToAssignmentsLeft;
    public decimal Score;

    public PartialSolution()
    {
        Days = Array.Empty<Day>();
        SupervisorAndReviewerIdToAssignmentsLeft = new Dictionary<(byte supervisorId, byte reviewerId), int>();
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

internal struct Day
{
    public int DayId;
    public Block[] Blocks;

    public readonly Day CreateDeepCopy()
    {
        var d = new Day();
        d.DayId = DayId;
        var b = Blocks;
        d.Blocks = new Block[b.Length];
        for (var i = 0; i < Blocks.Length; i++)
        {
            d.Blocks[i] = b[i].CreateDeepCopy();

        }
        return d;
    }
}

internal struct Block
{
    public int BlockId;
    public int Offset;
    public Assignment[] Assignments;

    public readonly Block CreateDeepCopy()
    {
        var b = new Block();
        b.BlockId = BlockId;
        b.Offset = Offset;
        b.Assignments = new Assignment[Assignments.Length];
        Array.Copy(Assignments, b.Assignments, Assignments.Length);
        return b;
    }
}

[StructLayout(LayoutKind.Explicit, Size = 4)]
internal struct Assignment
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
}