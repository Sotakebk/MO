using System.Runtime.InteropServices;

namespace Optimizer.Logic.Work.ChairPersonOptimization;

[StructLayout(LayoutKind.Explicit, Size = StructureSize)]
internal struct Position
{
    public const int StructureSize = 4 * sizeof(byte);

    [FieldOffset(0)] public byte Day = 0;
    [FieldOffset(1)] public byte Classroom = 0;
    [FieldOffset(2)] public byte Block = 0;
    [FieldOffset(3)] public byte BlockSize = 0;

    public Position(byte day, byte classroom, byte block, byte blockLength)
    {
        Day = day;
        Classroom = classroom;
        Block = block;
        BlockSize = blockLength;
    }
}

internal struct OptimizerAction : IAction<OptimizerState>
{
    public Position Position;
    public byte ChairPersonId;
    public float? ScoreField;
    public float? Score
    {
        readonly get => ScoreField;
        set => ScoreField = value;
    }

    public OptimizerAction()
    {
        Position = default;
        ChairPersonId = 0;
        ScoreField = null;
    }

    public readonly void ApplyToState(ref OptimizerState optimizerState)
    {
        optimizerState.Days[Position.Day].Classrooms[Position.Classroom].Blocks[Position.Block].SetAssignment(ChairPersonId);
        optimizerState.Score = Score;

        var count = 0;
        var dict1 = optimizerState.ChairPersonWorkingAssignmentsAsAnyRoleCount;

        if (dict1.TryGetValue(ChairPersonId, out var totalAssignments))
            count = totalAssignments;
        dict1[ChairPersonId] = count + Position.BlockSize;


        count = 0;
        var dict2 = optimizerState.ChairPersonWorkingAssignmentsAsChairPersonCounts;
        if (dict2.TryGetValue(ChairPersonId, out var totalAssignmentsAsChairPerson))
            count = totalAssignmentsAsChairPerson;
        dict2[ChairPersonId] = count + Position.BlockSize;

        optimizerState.DepthField++;
    }
}