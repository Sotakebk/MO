using System.Runtime.InteropServices;

namespace Optimizer.Logic.Work;

[StructLayout(LayoutKind.Explicit, Size = 3)]
internal struct AssignmentIndex
{
    public const int StructureSize = 3;

    [FieldOffset(0)] public byte Day = 0;
    [FieldOffset(1)] public byte Classroom = 0;
    [FieldOffset(2)] public byte Assignment = 0;

    public int Index => Day << 16 | Classroom << 8 | Assignment;

    public AssignmentIndex(byte day, byte classroom, byte assignment)
    {
        Day = day;
        Classroom = classroom;
        Assignment = assignment;
    }
}
