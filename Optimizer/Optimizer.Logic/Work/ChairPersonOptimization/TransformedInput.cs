namespace Optimizer.Logic.Work.ChairPersonOptimization;

public class TransformedInput
{
    public TransformedDay[] Days { get; set; }
    public HashSet<byte> AvailableChairPeople { get; set; }
    public Dictionary<(byte a, byte b), int> AssignmentsToBeDone { get; set; }

    public TransformedInput(Input input)
    {
        AvailableChairPeople = new HashSet<byte>(input.AvailableChairPersonIds);
        AssignmentsToBeDone = input.DefensesToAssign
            .GroupBy(pair => (a: Math.Min(pair.PromoterId, pair.ReviewerId), b: Math.Max(pair.PromoterId, pair.ReviewerId)))
            .Select(g => (key: g.Key, count: g.Sum(e => e.TotalCount)))
            .ToDictionary(g => g.key, g => g.count);
        Days = new TransformedDay[input.Days.Length];
        for (var i = 0; i < Days.Length; i++)
            Days[i] = new TransformedDay(input, input.Days[i]);
    }
}

public class TransformedDay
{
    public byte Id { get; set; }
    public int MaxBlocksInAnyClass { get; set; }
    public TransformedClassroom[] Classrooms { get; set; }

    public TransformedDay(Input input, InputDay inDay)
    {
        Id = inDay.Id;
        Classrooms = new TransformedClassroom[inDay.Classrooms.Length];
        for (var i = 0; i < inDay.Classrooms.Length; i++)
            Classrooms[i] = new TransformedClassroom(input, inDay.Classrooms[i]);

        MaxBlocksInAnyClass = inDay.Classrooms.Max(c => c.BlockLengths.Length);
    }
}

public class TransformedClassroom
{
    public byte Id { get; set; }
    public TransformedBlock[] Blocks { get; set; }

    public TransformedClassroom(Input input, InputClassroom inClassroom)
    {
        Id = inClassroom.Id;
        Blocks = new TransformedBlock[inClassroom.BlockLengths.Length];
        for (var i = 0; i < inClassroom.BlockLengths.Length; i++)
        {
            Blocks[i] = new TransformedBlock(input, inClassroom, (byte)i);
        }
    }
}

public class TransformedBlock
{
    public byte Id { get; private set; }
    public int First { get; private set; }
    public int Last { get; private set; }
    public int Length => Last - First + 1;
    public IReadOnlyDictionary<byte, TransformedPreference> ChairPeoplePreferences;

    public TransformedBlock(Input input, InputClassroom inClassroom, byte blockId)
    {
        Id = blockId;

        var accumulator = 0;
        for (var i = 0; i < blockId; i++)
        {
            accumulator += inClassroom.BlockLengths[i];
        }

        First = accumulator;
        Last = First + inClassroom.BlockLengths[blockId] - 1;

        var chairPeoplePreferences = new Dictionary<byte, TransformedPreference>();

        var slots = inClassroom.InputSlots[First..Last];
        var chairPeople = input.AvailableChairPersonIds;
        foreach (var slot in slots)
        {
            foreach (var preference in slot.Preferences.Where(p => chairPeople.Contains(p.PersonId)))
            {
                if (chairPeoplePreferences.TryGetValue(preference.PersonId, out var transformedPreference))
                {
                    AddToPreference(ref transformedPreference, preference.PreferenceType);
                }
                else
                {
                    transformedPreference = new TransformedPreference(0, 0, 0);
                    AddToPreference(ref transformedPreference, preference.PreferenceType);
                }

                // update struct in dictionary
                chairPeoplePreferences[preference.PersonId] = transformedPreference;
            }
        }

        ChairPeoplePreferences = chairPeoplePreferences;
    }

    private static void AddToPreference(ref TransformedPreference preference, PreferenceType type)
    {
        switch (type)
        {
            case PreferenceType.NotAllowed:
                preference.NotAllowedCount++;
                break;
            case PreferenceType.Preferred:
                preference.PreferredCount++;
                break;
            case PreferenceType.Undesired:
                preference.UndesiredCount++;
                break;
            default:
                throw new();
        }
    }
}

public struct TransformedPreference
{
    public int PreferredCount { get; set; }
    public int UndesiredCount { get; set; }
    public int NotAllowedCount { get; set; }

    public TransformedPreference(int preferredCount, int undesiredCount, int notAllowedCount)
    {
        PreferredCount = preferredCount;
        UndesiredCount = undesiredCount;
        NotAllowedCount = notAllowedCount;
    }
}