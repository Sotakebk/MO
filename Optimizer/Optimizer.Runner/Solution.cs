using Optimizer.Logic;
using Optimizer.Runner;
public class Solution
{
    private Dictionary<string, byte> _persons = new Dictionary<string, byte>();
    private Dictionary<byte, DefenseInfo> _defenseInfos = new Dictionary<byte, DefenseInfo>();
    private byte _nextId = 0;

    public Solution()
    {
    }

    public byte PersonId(string unique)
    {
        if (_persons.TryGetValue(unique, out var existId))
            return existId;
        else
        {
            var id = _nextId++;
            _persons.Add(unique, id);
            return id;
        }
    }

    public void AddDefenseInfo(byte personId, int dayId, int classroomId, byte assignmentA, byte assignmentB, byte chairPersonId)
    {
        if (!_defenseInfos.ContainsKey(personId))
        {
            _defenseInfos[personId] = new DefenseInfo();
        }

        _defenseInfos[personId].Add(new DefenseSlot
        {
            DayId = dayId,
            ClassroomId = classroomId,
            AssignmentA = assignmentA,
            AssignmentB = assignmentB,
            ChairPersonId = chairPersonId
        });
    }

    public Input GetOptimizerInput()
    {
        var input = new Input();

        // Available ChairPersons
        input.AvailableChairPersonIds = _persons.Values.ToArray();

        // DefensesToAssign
        input.DefensesToAssign = _defenseInfos.SelectMany(pair => pair.Value
            .GroupBy(defenseSlot => (reviewer: pair.Key, supervisor: pair.Key))
            .Select(group => new InputCombination
            {
                ReviewerId = group.Key.reviewer,
                PromoterId = group.Key.supervisor,
                TotalCount = group.Count()
            })).ToArray();

        // // Days
        // input.Days = _defenseInfos.Values
        //     .SelectMany(defenseInfo => defenseInfo
        //         .GroupBy(defenseSlot => defenseSlot.DayId)
        //         .Select(group => new InputDay
        //         {
        //             Id = group.Key,
        //             Classrooms = group
        //                 .GroupBy(defenseSlot => defenseSlot.ClassroomId)
        //                 .Select(classroomGroup => new InputClassroom(classroomGroup.Key, slots: 0)
        //                 {
        //                     InputSlots = classroomGroup
        //                         .Select(defenseSlot => new InputSlot
        //                         {
        //                             Preferences = new[]
        //                             {
        //                                 new InputSlotPreference
        //                                 {
        //                                     PersonId = defenseSlot.ReviewerId,
        //                                     PreferenceType = PreferenceType.Preferred
        //                                 }
        //                             }
        //                         }).ToArray()
        //                 }).ToArray()
        //         })).ToArray();

        return input;
    }


    private class DefenseInfo : List<DefenseSlot>
    {
    }

    private class DefenseSlot
    {
        public int DayId { get; set; }
        public int ClassroomId { get; set; }
        public byte AssignmentA { get; set; }
        public byte AssignmentB { get; set; }
        public byte ChairPersonId { get; set; }
    }
}
