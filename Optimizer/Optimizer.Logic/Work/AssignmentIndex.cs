using System.Runtime.InteropServices;

namespace Optimizer.Logic.Work;

[StructLayout(LayoutKind.Explicit, Size = 4)]
internal struct AssignmentIndex
{
    [FieldOffset(0)] public byte Day = 0;
    [FieldOffset(1)] public byte Block = 0;
    [FieldOffset(2)] public byte Assignment = 0;
    [FieldOffset(3)] private byte _allignment = 0; // waste of space

    public AssignmentIndex(byte day, byte block, byte assignment)
    {
        Day = day;
        Block = block;
        Assignment = assignment;
    }
}
