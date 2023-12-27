namespace Optimizer.Logic;

public struct Input
{
    public int[] ChairPersonIds;
    public InputCombination[] Combinations;
    public InputDay[] Days;
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
    public InputVacantBlock[] VacantBlocks;
}

public struct InputVacantBlock
{
    public int RoomId;
    public int Offset;
    public int SlotCount;
}