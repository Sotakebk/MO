namespace Optimizer.Logic.Work.AssignmentOptimization.Scores;

internal static class GeneralPeopleScore
{
    public const int MinPerfectAssignmentsPerDayCount = 6;
    public const int MaxPerfectAssignmentsPerDayCount = 9;
    public const int AcceptableMaxAssignmentsInBlockPerDay = 9;
    public const int AcceptableMaxAssignmentSpreadPerDay = 9;

    internal static float CalculateScore(OptimizerState state, TransformedInput tInput)
    {
        var operation = new Operation(state, tInput);
        operation.Work();
        return operation.CalculateScore();
    }

    public struct Metric
    {
        private static byte _count;
        public static byte Count => _count;

        private static Metric Create(float factor = 1.0f, bool depthDependant = false)
        {
            var metric = new Metric() { Id = _count++, Factor = factor, DepthDependant = depthDependant };
            Metrics[metric.Id] = metric;
            return metric;
        }

        public byte Id;
        public float Factor;
        public bool DepthDependant;

        public static readonly Metric[] Metrics = new Metric[16];

        public static Metric VacationDay = Create(0.0001f);
        public static Metric EveningStarting = Create(0.00001f, depthDependant: true);
        public static Metric DailyAssignmentsCount = Create(0.3f);
        public static Metric DailyOverspread = Create(0.9f);
        public static Metric SubsequentAssignmentsCount = Create(0.01f);

        public static Metric SwitchingClassesByChairPerson = Create(0.01f);
        public static Metric SwitchingClassesByOthers = Create(0.01f);

        public static Metric AssignmentGaps = Create(0.032f);
        public static Metric RoleSwitching = Create(0.0f);

        public static Metric ChairPersonAssignmentsLeft = Create(1f, depthDependant: true);
    }

    private class Operation
    {
        // TODO3: Żeby obrony zaczynały się od rana (czyli mozna liczyc ile jest dziury rano mają mniejsze penalty, niż dziury wieczorem), użycie kolejnego dnia obron powinno być również penalty

        private struct PersonAssignmentBlock
        {
            public int StartIndex;
            public int EndIndex;
            public int ClassroomIndex;
            public bool IsChairPerson;
        }

        private struct PersonPerDayMemory
        {
            public int? FirstAssignment;
            public int? LastAssignment;
            public readonly List<PersonAssignmentBlock> AssignmentBlocks;
            public int TotalAssignments;

            public PersonPerDayMemory()
            {
                FirstAssignment = null;
                LastAssignment = null;
                AssignmentBlocks = new List<PersonAssignmentBlock>();
                TotalAssignments = 0;
            }
        }

        private struct PersonMemory
        {
            public readonly PersonPerDayMemory[] Days;
            public int TotalAssignments;
            public int TotalChairAssignments;

            public PersonMemory(int days)
            {
                TotalAssignments = 0;
                TotalChairAssignments = 0;
                Days = new PersonPerDayMemory[days];
                for (var i = 0; i < days; i++)
                    Days[i] = new PersonPerDayMemory();
            }
        }

        private struct DayMemory
        {
            public int? FirstAssignment;
            public int? LastAssignment;
            public int TotalAssignmentsFilled;
        }

        private readonly PersonMemory[] _peopleMemory;
        private readonly DayMemory[] _daysMemory;
        private readonly OptimizerState _solution;
        private readonly TransformedInput _tInput;

        public Operation(OptimizerState solution, TransformedInput tInput)
        {
            _peopleMemory = new PersonMemory[tInput.PeopleCount];
            var daysInPartialSolution = solution.Days.Length;
            _daysMemory = new DayMemory[daysInPartialSolution];

            for (var i = 0; i < _peopleMemory.Length; i++)
            {
                _peopleMemory[i] = new PersonMemory(daysInPartialSolution);
            }

            _solution = solution;
            _tInput = tInput;
        }

        private void WorkForPerson(int dIndex, int cIndex, int sIndex, int personId, bool isChairPerson = false)
        {
            var memory = _peopleMemory[personId];
            _peopleMemory[personId].TotalAssignments++;

            memory.Days[dIndex].FirstAssignment ??= sIndex;
            memory.Days[dIndex].LastAssignment = sIndex;
            if (memory.Days[dIndex].AssignmentBlocks.Count == 0)
            {
                memory.Days[dIndex].AssignmentBlocks.Add(new PersonAssignmentBlock()
                {
                    ClassroomIndex = cIndex,
                    StartIndex = sIndex,
                    EndIndex = sIndex,
                    IsChairPerson = isChairPerson
                });
            }
            else
            {
                var lastBlockIndex = memory.Days[dIndex].AssignmentBlocks.Count - 1;
                var lastAssignmentBlock = memory.Days[dIndex].AssignmentBlocks[lastBlockIndex];
                if (lastAssignmentBlock.EndIndex == sIndex - 1
                    && lastAssignmentBlock.ClassroomIndex == cIndex
                    && lastAssignmentBlock.IsChairPerson == isChairPerson)
                {
                    // if block is continous, continue
                    lastAssignmentBlock.EndIndex = sIndex;
                    memory.Days[dIndex].AssignmentBlocks[lastBlockIndex] = lastAssignmentBlock;
                }
                else
                {
                    // start new block
                    memory.Days[dIndex].AssignmentBlocks.Add(new PersonAssignmentBlock()
                    {
                        ClassroomIndex = cIndex,
                        StartIndex = sIndex,
                        EndIndex = sIndex,
                        IsChairPerson = isChairPerson
                    });
                }

                memory.Days[dIndex].TotalAssignments++;
            }
        }

        public void Work()
        {
            for (var dIndex = 0; dIndex < _solution.Days.Length; dIndex++)
            {
                var solutionDay = _solution.Days[dIndex];
                var transformedDay = _tInput.Days[dIndex];
                var slotsInDay = transformedDay.MaxSlotsInDay;
                for (var sIndex = 0; sIndex < slotsInDay; sIndex++)
                {
                    for (int cIndex = 0; cIndex < solutionDay.Classrooms.Length; cIndex++)
                    {
                        // chronologically
                        var transformedClassroom = transformedDay.Classrooms[cIndex];
                        var classroom = solutionDay.Classrooms[cIndex];
                        if (classroom.Slots.Length <= sIndex) // less slots in class than in day
                            continue;

                        var assignment = classroom.Slots[sIndex];

                        if (assignment.HasValuesSet())
                        {
                            var chairPersonId = transformedClassroom.Slots[sIndex].ChairPersonId;
                            WorkForPerson(dIndex, cIndex, sIndex, assignment.A);
                            WorkForPerson(dIndex, cIndex, sIndex, assignment.B);
                            WorkForPerson(dIndex, cIndex, sIndex, chairPersonId, isChairPerson: true);
                            _daysMemory[dIndex].TotalAssignmentsFilled++;
                            _daysMemory[dIndex].FirstAssignment ??= sIndex;
                            _daysMemory[dIndex].LastAssignment = sIndex;
                        }
                    }
                }
            }
        }

        public float CalculateScore()
        {
            var depthPercentage = 1.0f * _solution.Depth / _solution.MaxDepth;
            var sum = 0f;
            var personMetrics = new float[Metric.Count];

            //var unassignedChairPersons = (float)_solution.ChairPersonAppearanceCount.Count(c => c.Value == 0);
            //sum -= unassignedChairPersons.ApplyMetric(Metric.UnusedChairPersons, depthPercentage);

            for (var personIndex = 0; personIndex < _tInput.PeopleCount; personIndex++)
            {
                for (var i = 0; i < Metric.Count; i++)
                    personMetrics[i] = 0;
                var person = _peopleMemory[personIndex];


                var assignmentsNotAsChairperson = _tInput.PersonWorkedAssignmentsAsNotAsChairperson[personIndex];
                if (_tInput.IsAssignedAsChairPersonLookupTable[personIndex] && assignmentsNotAsChairperson != 0)
                    personMetrics[Metric.ChairPersonAssignmentsLeft.Id] -= _solution.AssignmentsToPlaceLeftForPerson[personIndex] / assignmentsNotAsChairperson;

                for (var dayIndex = 0; dayIndex < person.Days.Length; dayIndex++)
                {
                    var day = person.Days[dayIndex];

                    // if no assignments in this day, ignore
                    if (day.FirstAssignment == null || day.LastAssignment == null)
                    {
                        // prize for empty day multiplied by slot count
                        personMetrics[Metric.VacationDay.Id] += 10.0f;
                        // personMetrics[Metric.VacationDay.Id] += 1.0f * _solution.Days[dayIndex].SlotCount;
                        continue;
                    }
                    else
                        personMetrics[Metric.VacationDay.Id] -= 10.0f;

                    // penaly for evening starting
                    personMetrics[Metric.EveningStarting.Id] -= day.FirstAssignment.Value;

                    var assignmentSpread = day.LastAssignment.Value - day.FirstAssignment.Value;
                    var assignmentsTotal = day.TotalAssignments;
                    // min(min(0, x-a), min(0, b-x)), b > a
                    // penalty for having assignments away from a perfect range
                    // having one assignment in a day makes no sense
                    // having too many is overworking

                    if (assignmentsTotal <= MaxPerfectAssignmentsPerDayCount)
                        personMetrics[Metric.DailyAssignmentsCount.Id] += assignmentsTotal;
                    else
                        personMetrics[Metric.DailyAssignmentsCount.Id] += MaxPerfectAssignmentsPerDayCount - assignmentsTotal;

                    // starting early with one assignment, and finishing with another very late makes no sense
                    // limit how far those may be by giving negative points for the value being too big
                    personMetrics[Metric.DailyOverspread.Id] += Math.Min(0, AcceptableMaxAssignmentSpreadPerDay - assignmentSpread);

                    for (var blockId = 0; blockId < day.AssignmentBlocks.Count; blockId++)
                    {
                        var assignmentBlock = day.AssignmentBlocks[blockId];
                        var assignmentsInBlock = assignmentBlock.EndIndex - assignmentBlock.StartIndex + 1;

                        // penalty for block being too large
                        if (assignmentsInBlock <= AcceptableMaxAssignmentsInBlockPerDay)
                            personMetrics[Metric.SubsequentAssignmentsCount.Id] += assignmentsInBlock;
                        else
                            personMetrics[Metric.SubsequentAssignmentsCount.Id] += AcceptableMaxAssignmentsInBlockPerDay - assignmentsInBlock;

                        if (blockId < day.AssignmentBlocks.Count - 1)
                        {
                            // penalty for empty hours
                            personMetrics[Metric.AssignmentGaps.Id] -= day.AssignmentBlocks[blockId + 1].StartIndex - day.AssignmentBlocks[blockId].EndIndex - 1;

                            // penalty for switching classes between two assignments
                            var block2 = day.AssignmentBlocks[blockId + 1];
                            if (assignmentBlock.ClassroomIndex != block2.ClassroomIndex)
                            {
                                if (assignmentBlock.IsChairPerson || block2.IsChairPerson)
                                    personMetrics[Metric.SwitchingClassesByChairPerson.Id] -= 1;
                                else
                                    personMetrics[Metric.SwitchingClassesByOthers.Id] -= 1;

                                // penalty for switching classes right between two assignments
                                //                                 if (assignmentBlock.EndIndex + 1 == block2.StartIndex)
                                //                                 {
                                //                                     personMetrics[Metric.SwitchingClasses.Id] -= 1.2;
                                //                                 }
                            }

                            // penalty for switching role
                            if (assignmentBlock.IsChairPerson != block2.IsChairPerson)
                                personMetrics[Metric.RoleSwitching.Id] -= 1;
                        }
                    }
                }

                for (var m = 0; m < Metric.Count; m++)
                {
                    sum += personMetrics[m].ApplyMetric(Metric.Metrics[m], depthPercentage);
                }
                // TODO: return or print?
                // PeopleMetrics[personIndex] = personMetrics;
            }

            return sum;
        }

        // private Dictionary<int, float[]> PeopleMetrics { get; set; } = new();
    }
}

internal static class MetricExtensions
{
    public static float ApplyMetric(this float value, GeneralPeopleScore.Metric metric, float percentage)
    {
        if (metric.DepthDependant)
            return value * metric.Factor * percentage;
        return value * metric.Factor;
    }
}