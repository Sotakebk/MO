using System.Runtime.InteropServices;

namespace Optimizer.Logic.Work.AssignmentOptimization;

internal struct OptimizerState : IState, ICopyable<OptimizerState>
{
    public Day[] Days;
    public Dictionary<(byte a, byte b), int> PairsToAssignLeft;
    public int[] AssignmentsToPlaceLeftForPerson;
    public float? Score { get; set; }
    public int Depth { get; set; }
    public int MaxDepth { get; set; }

    public readonly bool CheckIfIsCompleteSolution()
    {
        return Depth == MaxDepth;
    }

    public OptimizerState(Input input, TransformedInput tInput)
    {
        Days = new Day[input.Days.Length];
        for (var i = 0; i < input.Days.Length; i++)
        {
            Days[i] = new Day(input.Days[i]);
        }

        PairsToAssignLeft = input.DefensesToAssign
            .GroupBy(pair => (a: Math.Min(pair.PromoterId, pair.ReviewerId), b: Math.Max(pair.PromoterId, pair.ReviewerId)))
            .Select(g => (key: g.Key, count: g.Sum(e => e.TotalCount)))
            .ToDictionary(g => g.key, g => g.count);
        AssignmentsToPlaceLeftForPerson = new int[tInput.PeopleCount];
        for (var i = 0; i < tInput.PeopleCount; i++)
            AssignmentsToPlaceLeftForPerson[i] = input.DefensesToAssign
                .Where(d => d.PromoterId == i || d.ReviewerId == i).Sum(d => d.TotalCount);

        Depth = 0;
        MaxDepth = input.DefensesToAssign.Select(c => c.TotalCount).Sum();
        Score = float.NegativeInfinity;
    }


    public readonly OptimizerState CreateCopy()
    {
        var ps = this with
        {
            Days = new Day[Days.Length],
            PairsToAssignLeft = new Dictionary<(byte a, byte b), int>(PairsToAssignLeft),
            AssignmentsToPlaceLeftForPerson = new int[this.AssignmentsToPlaceLeftForPerson.Length]
        };

        Array.Copy(AssignmentsToPlaceLeftForPerson, ps.AssignmentsToPlaceLeftForPerson,
            AssignmentsToPlaceLeftForPerson.Length);

        for (var i = 0; i < ps.Days.Length; i++)
        {
            ps.Days[i] = Days[i].CreateDeepCopy();
        }

        return ps;
    }
}

internal struct Day
{
    public int Id;
    public Classroom[] Classrooms;

    public Day(InputDay inputDay)
    {
        Id = inputDay.Id;
        Classrooms = new Classroom[inputDay.Classrooms.Length];
        for (var i = 0; i < Classrooms.Length; i++)
        {
            Classrooms[i] = new Classroom(inputDay.Classrooms[i]);
        }
    }

    public readonly Day CreateDeepCopy()
    {
        var d = this with { Classrooms = new Classroom[Classrooms.Length] };
        for (var i = 0; i < Classrooms.Length; i++)
            d.Classrooms[i] = Classrooms[i].CreateDeepCopy();
        return d;
    }
}

internal struct Classroom
{
    public int Id;
    public Slot[] Slots;

    public Classroom(InputClassroom inputClassroom)
    {
        Id = inputClassroom.Id;
        Slots = new Slot[inputClassroom.InputSlots.Length];
    }

    public readonly Classroom CreateDeepCopy()
    {
        var c = this with { Slots = new Slot[Slots.Length] };
        Array.Copy(Slots, c.Slots, Slots.Length);
        return c;
    }
}

[StructLayout(LayoutKind.Explicit, Size = sizeof(byte) * 2)]
internal struct Slot
{
    [FieldOffset(1)] public byte A = 0;
    [FieldOffset(2)] public byte B = 0;

    public Slot()
    {
    }

    public void SetAssignment(byte a, byte b)
    {
#if DEBUG
        if (a == b)
            throw new($"{nameof(a)} and {nameof(b)} cannot be equal");


        if (a > b)
            throw new($"{nameof(a)} > {nameof(b)} is not allowed");

        if (HasValuesSet())
            throw new($"assignment values were already set!");
#endif
        A = a;
        B = b;
    }

    public readonly bool HasValuesSet() => A != B;

    public readonly override string ToString()
    {
        return $"{A} {B}";
    }
}
