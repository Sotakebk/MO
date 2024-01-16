using Optimizer.Logic.Extensions;

namespace Optimizer.Logic.Work.Heuristics;

internal static class GeneralPeopleHeuristic
{
    public const int MinPerfectAssignmentsPerDayCount = 6;
    public const int MaxPerfectAssignmentsPerDayCount = 9;
    public const int AcceptableMaxAssignmentsInBlockPerDay = 9;
    public const int AcceptableMaxAssignmentSpreadPerDay = 9;

    internal static float CalculateScore(PartialSolution solution)
    {
        var operation = new Operation(solution);
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

        public static Metric VacationDay = Create(0.001f);
        public static Metric EveningStarting = Create(0.00f, depthDependant: true);
        public static Metric DailyAssignmentsCount = Create(0.25f);
        public static Metric DailyOverspread = Create(0.01f);
        public static Metric SubsequentAssignmentsCount = Create(0.01f);

        public static Metric SwitchingClassesByChairPerson = Create(0.02f);
        public static Metric SwitchingClassesByOthers = Create(0.01f);

        public static Metric AssignmentGaps = Create(0.032f);
        public static Metric RoleSwitching = Create(0.018f);
        public static Metric ChairpersonStd = Create(0.02f);
        public static Metric UnusedChairPersons = Create(0.12f, depthDependant: true);
    }


    private class Operation
    {
        // TODO3: Żeby obrony zaczynały się od rana (czyli mozna liczyc ile jest dziury rano mają mniejsze penalty, niż dziury wieczorem), użycie kolejnego dnia obron powinno być również penalty
        // TODO4: Plusik za wypełnienie promotora_recenzenta i supervisora

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
            public readonly bool Exists;

            public PersonMemory(int days)
            {
                Exists = true;
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
        private readonly PartialSolution _solution;

        public Operation(PartialSolution solution)
        {
            _peopleMemory = new PersonMemory[byte.MaxValue + 1];
            var daysInPartialSolution = solution.Days.Length;
            _daysMemory = new DayMemory[daysInPartialSolution];

            for (var i = 0; i < solution.PeopleIds.Count; i++)
            {
                _peopleMemory[solution.PeopleIds[i]] = new PersonMemory(daysInPartialSolution);
            }

            _solution = solution;
        }

        public void Work()
        {
            for (var dIndex = 0; dIndex < _solution.Days.Length; dIndex++)
            {
                var solutionDay = _solution.Days[dIndex];
                for (var aIndex = 0; aIndex < solutionDay.SlotCount; aIndex++)
                {
                    for (int cIndex = 0; cIndex < solutionDay.Classrooms.Length; cIndex++)
                    {
                        // chronologically
                        var assignment = solutionDay.Classrooms[cIndex].Assignments[aIndex];

                        void WorkForPerson(int personId, bool isChairPerson = false)
                        {
                            var memory = _peopleMemory[personId];
                            _peopleMemory[personId].TotalAssignments++;
                            if (isChairPerson)
                                _peopleMemory[personId].TotalChairAssignments++;

                            memory.Days[dIndex].FirstAssignment ??= aIndex;
                            memory.Days[dIndex].LastAssignment = aIndex;
                            if (memory.Days[dIndex].AssignmentBlocks.Count == 0)
                            {
                                memory.Days[dIndex].AssignmentBlocks.Add(new PersonAssignmentBlock()
                                {
                                    ClassroomIndex = cIndex,
                                    StartIndex = aIndex,
                                    EndIndex = aIndex,
                                    IsChairPerson = isChairPerson
                                });
                            }
                            else
                            {
                                var lastBlockIndex = memory.Days[dIndex].AssignmentBlocks.Count - 1;
                                var lastAssignmentBlock = memory.Days[dIndex].AssignmentBlocks[lastBlockIndex];
                                if (lastAssignmentBlock.EndIndex == aIndex - 1
                                    && lastAssignmentBlock.ClassroomIndex == cIndex
                                    && lastAssignmentBlock.IsChairPerson == isChairPerson)
                                {
                                    // if block is continous, continue
                                    lastAssignmentBlock.EndIndex = aIndex;
                                    memory.Days[dIndex].AssignmentBlocks[lastBlockIndex] = lastAssignmentBlock;
                                }
                                else
                                {
                                    // start new block
                                    memory.Days[dIndex].AssignmentBlocks.Add(new PersonAssignmentBlock()
                                    {
                                        ClassroomIndex = cIndex,
                                        StartIndex = aIndex,
                                        EndIndex = aIndex,
                                        IsChairPerson = isChairPerson
                                    });
                                }

                                memory.Days[dIndex].TotalAssignments++;
                            }
                        }

                        if (assignment.HasValuesSet())
                        {
                            WorkForPerson(assignment.SupervisorId);
                            WorkForPerson(assignment.ReviewerId);
                            WorkForPerson(assignment.ChairPersonId, isChairPerson: true);
                            _daysMemory[dIndex].TotalAssignmentsFilled++;
                            _daysMemory[dIndex].FirstAssignment ??= aIndex;
                            _daysMemory[dIndex].LastAssignment = aIndex;
                        }
                    }
                }
            }
        }

        public float CalculateScore()
        {
            var depthPercentage = 1.0f * _solution.CurrentDepth / _solution.MaxDepth;
            var sum = 0f;
            var personMetrics = new float[Metric.Count];

            var chairPersonStdDev = _peopleMemory.Where(p => p.TotalChairAssignments > 0).Select(p => (float)p.TotalChairAssignments).StandardDeviation();
            sum -= chairPersonStdDev.ApplyMetric(Metric.ChairpersonStd, depthPercentage);

            var unassignedChairPersons = (float)_solution.ChairPersonAppearanceCount.Count(c => c.Value == 0);
            sum -= unassignedChairPersons.ApplyMetric(Metric.UnusedChairPersons, depthPercentage);

            for (var personIndex = 0; personIndex <= byte.MaxValue; personIndex++)
            {
                for (var i = 0; i < Metric.Count; i++)
                    personMetrics[i] = 0;
                var person = _peopleMemory[personIndex];
                if (!person.Exists)
                    continue;
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
                    }else
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

static class MetricExtensions
{
    public static float ApplyMetric(this float value, GeneralPeopleHeuristic.Metric metric, float percentage)
    {
        if (metric.DepthDependant)
            return value * metric.Factor * percentage;
        return value * metric.Factor;
    }
}