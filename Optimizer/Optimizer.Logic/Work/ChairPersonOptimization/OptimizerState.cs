using System.Runtime.InteropServices;

namespace Optimizer.Logic.Work.ChairPersonOptimization;

public struct OptimizerState : IState, ICopyable<OptimizerState>
{
    public StateDay[] Days;
    public Dictionary<byte, int> ChairPersonWorkingAssignmentsAsAnyRoleCount;
    public Dictionary<byte, int> ChairPersonWorkingAssignmentsAsChairPersonCounts;
    public float? _score = null;
    public int DepthField = 0;
    public int MaxDepthField = 0;

    #region IState

    public readonly int Depth => DepthField;
    public readonly int MaxDepth => MaxDepthField;

    public float? Score
    {
        readonly get => _score;
        set => _score = value;
    }

    #endregion IState

    public readonly bool CheckIfIsCompleteSolution()
    {
        return DepthField >= MaxDepthField;
    }

    public float CalculateScore()
    {
        return 0;
    }

    public OptimizerState()
    {
        Days = Array.Empty<StateDay>();
        ChairPersonWorkingAssignmentsAsAnyRoleCount = new();
        ChairPersonWorkingAssignmentsAsChairPersonCounts = new();
    }

    public OptimizerState(TransformedInput tInput)
    {
        ChairPersonWorkingAssignmentsAsChairPersonCounts = new Dictionary<byte, int>();
        Days = new StateDay[tInput.Days.Length];
        ChairPersonWorkingAssignmentsAsAnyRoleCount = new Dictionary<byte, int>();

        void MaybeUpdateWorkingAssignmentCount(Dictionary<byte, int> dict, byte id, int count)
        {
            if (!tInput.AvailableChairPeople.Contains(id))
                return;

            if (dict.TryGetValue(id, out var assignmentCount))
            {
                dict[id] = assignmentCount + count;
            }
            else
            {
                dict[id] = count;
            }
        }

        foreach (var element in tInput.AssignmentsToBeDone)
        {
            var (a, b) = element.Key;
            MaybeUpdateWorkingAssignmentCount(ChairPersonWorkingAssignmentsAsAnyRoleCount, a, element.Value);
            MaybeUpdateWorkingAssignmentCount(ChairPersonWorkingAssignmentsAsAnyRoleCount, b, element.Value);
        }

        for (var i = 0; i < tInput.Days.Length; i++)
        {
            Days[i] = new StateDay(tInput.Days[i]);
        }

        // number of blocks to assign
        MaxDepthField = tInput.Days.Sum(d => d.Classrooms.Sum(c => c.Blocks.Length));
        DepthField = 0;

        _score = null;
    }

    public readonly OptimizerState CreateCopy()
    {
        var original = this;
        var copy = new OptimizerState
        {
            Days = new StateDay[original.Days.Length]
        };
        for (var i = 0; i < original.Days.Length; i++)
        {
            copy.Days[i] = StateDay.Clone(original.Days[i]);
        }

        copy.ChairPersonWorkingAssignmentsAsAnyRoleCount = new Dictionary<byte, int>(original.ChairPersonWorkingAssignmentsAsAnyRoleCount);
        copy.ChairPersonWorkingAssignmentsAsChairPersonCounts = new Dictionary<byte, int>(original.ChairPersonWorkingAssignmentsAsChairPersonCounts);

        copy.MaxDepthField = original.MaxDepthField;
        copy.DepthField = original.DepthField;
        copy._score = original._score;

        return copy;
    }
}

public struct StateDay
{
    public int Id;
    public StateClassroom[] Classrooms;

    public StateDay(TransformedDay day)
    {
        Id = day.Id;
        Classrooms = new StateClassroom[day.Classrooms.Length];
        for (var i = 0; i < Classrooms.Length; i++)
            Classrooms[i] = new StateClassroom(day.Classrooms[i]);
    }

    public static StateDay Clone(StateDay original)
    {
        var copy = new StateDay
        {
            Id = original.Id,
            Classrooms = new StateClassroom[original.Classrooms.Length]
        };
        for (var i = 0; i < original.Classrooms.Length; i++)
            copy.Classrooms[i] = StateClassroom.CreateDeepCopy(original.Classrooms[i]);

        return copy;
    }
}

public struct StateClassroom
{
    public byte Id;
    public StateBlock[] Blocks;

    public StateClassroom(TransformedClassroom classroom)
    {
        Id = classroom.Id;
        Blocks = new StateBlock[classroom.Blocks.Length];
    }

    public static StateClassroom CreateDeepCopy(StateClassroom original)
    {
        var copy = new StateClassroom
        {
            Id = original.Id,
            Blocks = new StateBlock[original.Blocks.Length]
        };
        Array.Copy(original.Blocks, copy.Blocks, original.Blocks.Length);
        return copy;
    }
}

[StructLayout(LayoutKind.Explicit, Size = sizeof(byte) * 2)]
public struct StateBlock
{
    [FieldOffset(0)] private byte _chairPersonId = 0;
    [FieldOffset(1)] private byte _flag = 0;

    public readonly bool IsAssigned => _flag != 0;

    public readonly byte ChairPersonId => _chairPersonId;

    public StateBlock()
    {
    }

    public void SetAssignment(byte p)
    {
#if DEBUG
        if (_flag == 1)
            throw new($"assignment values were already set!");
#endif
        _chairPersonId = p;
        _flag = 1;
    }
}
