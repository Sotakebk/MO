using System.Runtime.InteropServices;

namespace Optimizer.Logic.Work.AssignmentOptimization;

[StructLayout(LayoutKind.Explicit, Size = StructureSize)]
internal struct Position
{
    public const int StructureSize = 3 * sizeof(byte);

    [FieldOffset(0)] public byte Day = 0;
    [FieldOffset(1)] public byte Classroom = 0;
    [FieldOffset(2)] public byte Slot = 0;

    public Position(byte day, byte classroom, byte slot)
    {
        Day = day;
        Classroom = classroom;
        Slot = slot;
    }

    public readonly override int GetHashCode()
    {
        return (Day << 16) | (Classroom << 8) | Slot;
    }

    public override string ToString()
    {
        return $"d{Day}c{Classroom}s{Slot}";
    }
}

internal struct OptimizerAction : IAction<OptimizerState>
{
    public Position SlotId;
    public byte A;
    public byte B;
    public float? _score;

    public float? Score
    {
        readonly get => _score;
        set => _score = value;
    }

    public OptimizerAction(byte a, byte b, Position slotId)
    {
        A = a;
        B = b;
        _score = null;
        SlotId = slotId;
    }

    public readonly override string ToString()
    {
        return $"Action: score:{Score}";
    }

    public readonly void ApplyToState(ref OptimizerState state)
    {
        var assignments = state.Days[SlotId.Day]
            .Classrooms[SlotId.Classroom]
            .Slots;

        assignments[SlotId.Slot].SetAssignment(A, B);

        var count = state.PairsToAssignLeft[(A, B)];
        state.PairsToAssignLeft[(A, B)] = count - 1;

        state.AssignmentsToPlaceLeftForPerson[A]--;
        state.AssignmentsToPlaceLeftForPerson[B]--;

        state.Score = Score;
        state.Depth++;
    }
}