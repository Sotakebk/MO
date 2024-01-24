namespace Optimizer.Logic.Work.AssignmentOptimization;

internal class TransformedInput
{
    public byte[] AllPeopleIds;
    public int PeopleCount;
    public Dictionary<byte, int> PersonWorkedAssignmentsAsAnyRoleCount;
    public int[] PersonWorkedAssignmentsAsNotAsChairperson;
    public bool[] IsAssignedAsChairPersonLookupTable;
    public TransformedDay[] Days;

    public TransformedInput(Input input, ChairPersonOptimization.OptimizerOutput chairPersonOptimizerOutput)
    {
        AllPeopleIds = input.DefensesToAssign.Select(c => c.ReviewerId)
            .Union(input.DefensesToAssign.Select(c => c.PromoterId))
            .Union(input.AvailableChairPersonIds.Select(c => c))
            .OrderBy(i => i)
            .Select(i => i)
            .ToArray();
        PeopleCount = AllPeopleIds.Length;

        PersonWorkedAssignmentsAsAnyRoleCount = new(chairPersonOptimizerOutput.PersonWorkedAssignmentsAsAnyRoleCount);

        PersonWorkedAssignmentsAsNotAsChairperson = new int [PeopleCount];
        for (var i = 0; i < PeopleCount; i++) 
            PersonWorkedAssignmentsAsNotAsChairperson[i] = input.DefensesToAssign.Where(combination => combination.PromoterId == i || combination.ReviewerId == i).Sum(c => c.TotalCount);


        IsAssignedAsChairPersonLookupTable = new bool[PeopleCount];
        for (var i = 0; i < PeopleCount; i++)
            IsAssignedAsChairPersonLookupTable[i] = chairPersonOptimizerOutput.Days.Any(d =>
                d.Classrooms.Any(c => c.OutputBlocks.Any(b => b.ChairPersonId == i)));

        Days = new TransformedDay[input.Days.Length];
        for (var i = 0; i < Days.Length; i++)
            Days[i] = new TransformedDay(input.Days[i], chairPersonOptimizerOutput.Days.Single(d => d.Id == input.Days[i].Id));
    }
}

internal class TransformedDay
{
    public int Id;
    public TransformedClassroom[] Classrooms;
    public int MaxSlotsInDay;

    public TransformedDay(InputDay inputDay, ChairPersonOptimization.OutputDay chairPersonOptimizerDay)
    {
        Id = inputDay.Id;
        Classrooms = new TransformedClassroom[inputDay.Classrooms.Length];
        for (var i = 0; i < inputDay.Classrooms.Length; i++)
            Classrooms[i] = new TransformedClassroom(inputDay.Classrooms[i],
                chairPersonOptimizerDay.Classrooms.Single(c => c.Id == inputDay.Classrooms[i].Id));
        MaxSlotsInDay = inputDay.Classrooms.Max(c => c.InputSlots.Length);
    }
}

internal class TransformedClassroom
{
    public int Id;
    public TransformedSlot[] Slots;

    public TransformedClassroom(InputClassroom inputClassroom,
        ChairPersonOptimization.OutputClassroom chairPersonOptimizerClassroom)
    {
        Id = inputClassroom.Id;
        Slots = new TransformedSlot[inputClassroom.InputSlots.Length];
        for (var i = 0; i < Slots.Length; i++)
        {
            var block = chairPersonOptimizerClassroom.OutputBlocks.Single(b => b.Start <= i && b.End >= i);
            Slots[i] = new TransformedSlot(inputClassroom.InputSlots[i], block);
        }
    }
}

internal struct TransformedSlot
{
    public byte ChairPersonId;
    public PreferenceType?[] Preferences;

    public TransformedSlot(InputSlot inputSlot, ChairPersonOptimization.OutputBlock chairPersonOptimizerBlock)
    {
        ChairPersonId = chairPersonOptimizerBlock.ChairPersonId ?? throw new();
        Preferences = new PreferenceType?[byte.MaxValue + 1];
        foreach (var preference in inputSlot.Preferences)
        {
            Preferences[preference.PersonId] = preference.PreferenceType;
        }
    }

    public readonly PreferenceType? GetPreferenceForPerson(byte personId) => Preferences[personId];
}