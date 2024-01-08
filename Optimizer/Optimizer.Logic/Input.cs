namespace Optimizer.Logic;

public struct Input
{
    public int[] ChairPersonIds;
    public InputCombination[] Combinations;
    public InputDay[] Days;
    public (int day, int roomId, int slotId)[] ForbiddenSlots;
}

public struct InputCombination
{
    public int ReviewerId;
    public int PromoterId;
    public int TotalCount;
}

public struct InputDay
{
    public int Id;
    public int SlotCount;
    public InputClassroom[] Classrooms;
}

public struct InputClassroom
{
    public int RoomId;
}