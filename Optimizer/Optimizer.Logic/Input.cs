namespace Optimizer.Logic;

public class Input
{
    public byte[] AvailableChairPersonIds { get; set; } = Array.Empty<byte>();
    public InputCombination[] DefensesToAssign { get; set; } = Array.Empty<InputCombination>();
    public InputDay[] Days { get; set; } = Array.Empty<InputDay>();
}

public class InputCombination
{
    public byte ReviewerId { get; set; }
    public byte PromoterId { get; set; }
    public int TotalCount { get; set; }
}

public class InputDay
{
    public byte Id { get; set; }
    public InputClassroom[] Classrooms { get; set; } = Array.Empty<InputClassroom>();
}

public class InputClassroom
{
    public byte Id { get; set; }
    public int[] BlockLengths { get; set; }
    public InputSlot[] InputSlots { get; set; }

    public InputClassroom(byte id, byte slots, params int[] blockLengths)
    {
        Id = id;
        InputSlots = new InputSlot[slots];
        for (var i = 0; i < slots; i++)
            InputSlots[i] = new InputSlot();
        if (slots != blockLengths.Sum())
            throw new("slot count not equal to sum of block lengths");
        BlockLengths = blockLengths;
    }
}

public struct InputSlot
{
    public InputSlotPreference[] Preferences = Array.Empty<InputSlotPreference>();

    public InputSlot()
    {
    }
}

public struct InputSlotPreference
{
    public byte PersonId;
    public PreferenceType PreferenceType;
}

public enum PreferenceType : byte
{
    Preferred,
    Undesired,
    NotAllowed,
}