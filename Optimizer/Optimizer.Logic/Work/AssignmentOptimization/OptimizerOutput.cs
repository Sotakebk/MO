namespace Optimizer.Logic.Work.AssignmentOptimization;

public class OptimizerOutput : IOutput
{
    public float Score { get; set; }

    public OutputDay[] Days { get; set; }

    internal OptimizerOutput(OptimizerState state, TransformedInput tInput)
    { 
        Score = state.Score ?? float.NaN;
        Days = new OutputDay[state.Days.Length];
        for (var i = 0; i < Days.Length; i++)
            Days[i] = new OutputDay(state.Days[i], tInput.Days.Single(d => d.Id == state.Days[i].Id));
    }
}

public struct OutputDay
{
    public int Id { get; set; }
    public SolutionClassroom[] Classrooms { get; set; }

    internal OutputDay(Day stateDay, TransformedDay tDay)
    {
        Id = stateDay.Id;
        Classrooms = new SolutionClassroom[stateDay.Classrooms.Length];
        for (var i = 0; i < Classrooms.Length; i++)
            Classrooms[i] = new SolutionClassroom(stateDay.Classrooms[i],
                tDay.Classrooms.Single(c => c.Id == stateDay.Classrooms[i].Id));
    }

    public readonly override string ToString()
    {
        return $"Day ID: {Id} with {Classrooms.Length} classrooms";
    }
}

public struct SolutionClassroom
{
    public int Id { get; set; }
    public SolutionSlot[] Slots { get; set; }

    internal SolutionClassroom(Classroom classroom, TransformedClassroom tClassroom)
    {
        Id = classroom.Id;
        Slots = new SolutionSlot[classroom.Slots.Length];
        for (var i = 0; i < Slots.Length; i++)
            Slots[i] = new SolutionSlot(classroom.Slots[i], tClassroom.Slots[i]);
    }

    public readonly override string ToString()
    {
        return $"ID:'{Id}', [{string.Join(", ", Slots.Select(a => $"({a.ToString()})")).Substring(0, 20)}]";
    }
}

public struct SolutionSlot
{
    public int A { get; set; }
    public int B { get; set; }
    public int ChairPersonId { get; set; }

    internal SolutionSlot(Slot slot, TransformedSlot tSlot)
    {
        A = slot.A;
        B = slot.B;
        ChairPersonId = tSlot.ChairPersonId;
    }

    public readonly override string ToString()
    {
        return A == B ? $"nothing assigned, cp: '{ChairPersonId}'" : $"a: '{A}', b: '{B}', cp: '{ChairPersonId}'";
    }
}