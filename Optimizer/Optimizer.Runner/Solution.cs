using Optimizer.Logic;

namespace Optimizer.Runner;

public class Solution
{
    private record DayRoom(byte Day, byte Room);

    private readonly Dictionary<string, byte> _persons = new();
    private readonly Dictionary<DayRoom, byte[]> _slots = new();
    private readonly Dictionary<(byte, DateOnly day), List<TimePeriod>> _personExclusions = new();
    private readonly HashSet<byte> _chairPersons = new();
    private readonly List<DefenseInfo> _defenseInfos = new();
    private byte _nextId;

    private byte GetNameUniqueId(string unique)
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

    public Input GetOptimizerInput(DateOnly beginDate)
    {
        var input = new Input
        {
            // Available ChairPersons
            AvailableChairPersonIds = _chairPersons.ToArray(),
            // DefensesToAssign
            DefensesToAssign = _defenseInfos
                .Take(84) // TODO: Remove
                .Select(pair => (reviewer: pair.ReviewerId, supervisor: pair.SupervisorId))
                .GroupBy(grouped => grouped)
                .Select(group => new InputCombination
                {
                    ReviewerId = group.Key.reviewer,
                    PromoterId = group.Key.supervisor,
                    TotalCount = group.Count()
                }).ToArray()
        };

        // Days
        byte id = 0;
        input.Days =
            _slots
                .Select(pair => (day: pair.Key.Day, room: pair.Key.Room, slots: pair.Value))
                .GroupBy(tuple => tuple.day)
                .Select(tuples =>
                {
                    var dayIndex = id++;
                    return new InputDay()
                    {
                        Id = dayIndex,
                        Classrooms = tuples.Select(tuple =>
                        {
                            var classroom = new InputClassroom(tuple.room, tuple.slots);
                            ApplyPersonPreferences(beginDate, dayIndex, classroom);
                            return classroom;
                        }).ToArray()
                    };
                }).ToArray();
        return input;
    }

    private void ApplyPersonPreferences(DateOnly beginDate, byte dayIndex, InputClassroom classroom)
    {
        foreach (var day in _personExclusions
                     .Where(pair => pair.Key.day == beginDate.AddDays(dayIndex))
                     .GroupBy(pair => pair.Key.day))
        {
            foreach (var setting in day)
            {
                var slotsLength = classroom.InputSlots.Length;
                foreach (var slotIndex in setting.Value.SlotIndices().TakeWhile(t => t < slotsLength))
                {
                    classroom.InputSlots[slotIndex].Preferences.Add(new InputSlotPreference() { PersonId = setting.Key.Item1, PreferenceType = PreferenceType.NotAllowed });
                }
            }
        }
    }

    private class DefenseInfo
    {
        public DefenseInfo(string student, string title, byte supervisorId, byte reviewerId)
        {
            Student = student;
            Title = title;
            SupervisorId = supervisorId;
            ReviewerId = reviewerId;
        }

        public string Student { get; set; }
        public string Title { get; set; }
        public byte SupervisorId { get; set; }
        public byte ReviewerId { get; set; }
    }

    public void AddChairPerson(string chairpersonName)
    {
        _chairPersons.Add(GetNameUniqueId(chairpersonName));
    }

    public void AddDefenseInfo(string studentName, string title, string supervisor, string reviewer)
    {
        _defenseInfos.Add(new DefenseInfo(studentName, title, GetNameUniqueId(supervisor), GetNameUniqueId(reviewer)));
    }

    public void AddRoomInfo(byte day, byte room, byte[] defenses)
    {
        if (_slots.ContainsKey(new DayRoom(day, room)))
            throw new UserFriendlyException(
                $"Configuration for room {room + 1} in day {day + 1} already exist",
                $"Konfiguracja dla sali {room + 1} w dniu {day + 1} już istnieje. Sprawdź duplikaty."
            );
        _slots[new DayRoom(day, room)] = defenses;
    }

    public void AddAbsence(string person, DateOnly day, TimePeriod timeSpan)
    {
        var key = (GetNameUniqueId(person), day);

        if (_personExclusions.TryGetValue(key, out var value))
            value.Add(timeSpan);
        else
            _personExclusions[key] = new List<TimePeriod> { timeSpan };
    }

    public class FullDefenseInfo
    {
        public FullDefenseInfo()
        {
            Student = "";
            Title = "";
            Supervisor = "";
            Reviewer = "";
            Chairperson = "";
        }

        public FullDefenseInfo(string student, string title, string supervisor, string reviewer, string chairperson)
        {
            Student = student;
            Title = title;
            Supervisor = supervisor;
            Reviewer = reviewer;
            Chairperson = chairperson;
        }

        public string Student { get; set; }
        public string Title { get; set; }
        public string Supervisor { get; set; }
        public string Reviewer { get; set; }
        public string Chairperson { get; set; }
    }

    public FullDefenseInfo GetFullDefenseInfo(int aId, int bId, int chairPersonId)
    {
        var aName = _persons.First(x => x.Value == aId).Key;
        var bName = _persons.First(x => x.Value == bId).Key;
        var partialDefenseInfo = _defenseInfos.FirstOrDefault(x => 
            (x.SupervisorId == aId && x.ReviewerId == bId) ||
            (x.SupervisorId == bId && x.ReviewerId == aId));

        if (partialDefenseInfo is null)
        {
            throw new UserFriendlyException(
                "No defense with id combination " + aName + " " + bName,
                "Nie ma obrony dla kombinacji id " + aName + " " + bName
            );
        }

        var defaultOrder = partialDefenseInfo.SupervisorId == aId;

        _defenseInfos.Remove(partialDefenseInfo);

        return new FullDefenseInfo(
            partialDefenseInfo.Student,
            partialDefenseInfo.Title,
            defaultOrder ? aName : bName,
            defaultOrder ? bName : aName,
            _persons.First(x => x.Value == chairPersonId).Key
        );
    }
}