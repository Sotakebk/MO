namespace Optimizer.Logic.Work.ChairPersonOptimization;

public class OptimizerOutput : IOutput
{
    public float Score { get; set; }

    public OutputDay[] Days;
    public Dictionary<byte, int> PersonWorkedAssignmentsAsAnyRoleCount;

    public OptimizerOutput(OptimizerState state, Input input, TransformedInput tInput)
    {
        Days = new OutputDay[state.Days.Length];
        Score = state.Score ?? float.NegativeInfinity;
        for (var i = 0; i < Days.Length; i++)
        {
            var dayId = state.Days[i].Id;
            Days[i] = new OutputDay(
                state.Days[i],
                input.Days.Single(d => d.Id == dayId),
                tInput.Days.Single(d => d.Id == dayId));
        }

        PersonWorkedAssignmentsAsAnyRoleCount = new(state.ChairPersonWorkingAssignmentsAsAnyRoleCount);
    }
}

public class OutputDay
{
    public int Id { get; set; }
    public OutputClassroom[] Classrooms { get; set; }

    public OutputDay(StateDay day, InputDay iDay, TransformedDay tDay)
    {
        Id = day.Id;
        Classrooms = new OutputClassroom[day.Classrooms.Length];
        for (var i = 0; i < Classrooms.Length; i++)
        {
            var classId = day.Classrooms[i].Id;
            Classrooms[i] = new OutputClassroom(
                day.Classrooms[i],
                iDay.Classrooms.Single(c => c.Id == classId),
                tDay.Classrooms.Single(c => c.Id == classId));
        }
    }
}

public class OutputClassroom
{
    public byte Id { get; set; }
    public OutputBlock[] OutputBlocks { get; set; }

    public OutputClassroom(StateClassroom classroom, InputClassroom iClassroom, TransformedClassroom tClassroom)
    {
        Id = classroom.Id;
        OutputBlocks = new OutputBlock[classroom.Blocks.Length];
        for (var i = 0; i < OutputBlocks.Length; i++)
        {
            OutputBlocks[i] = new OutputBlock(
                classroom.Blocks[i], // trust that they are not out of order!
                iClassroom,
                tClassroom.Blocks[i]);
        }
    }
}

public class OutputBlock
{
    public byte? ChairPersonId { get; set; }
    public int Id { get; set; }
    public int Start { get; set; }
    public int End { get; set; }
    public int Length => End - Start + 1;

    public OutputBlock(StateBlock stateBlock, InputClassroom iClassroom, TransformedBlock tBlock)
    {
        ChairPersonId = stateBlock.IsAssigned ? stateBlock.ChairPersonId : null;
        Id = tBlock.Id;
        Start = tBlock.First;
        End = tBlock.Last;
    }
}