namespace Optimizer.Logic.Work.Score.Heuristics;

public class GeneralPeopleHeuristic : IHeuristic
{
    public const int MinPerfectAssignmentsPerDayCount = 6;
    public const int MaxPerfectAssignmentsPerDayCount = 10;
    public const int AcceptableMaxAssignmentsInBlockPerDay = 6;
    public const int AcceptableMaxAssignmentSpreadPerDay = 14;

    decimal IHeuristic.CalculateScore(PartialSolution solution)
    {
        var operation = new Operation(solution);
        operation.Work();
        return operation.CalculateScore();
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
        }

        private struct PersonPerDayMemory
        {
            public int? FirstAssignment;
            public int? LastAssignment;
            public List<PersonAssignmentBlock> AssignmentBlocks;
            public int TotalAssignments;

            public PersonPerDayMemory()
            {
                FirstAssignment = null;
                LastAssignment = null;
                AssignmentBlocks = new(2);
                TotalAssignments = 0;
            }
        }

        private struct PersonMemory
        {
            public readonly PersonPerDayMemory[] Days;
            public int TotalAssignments;

            public PersonMemory(int days)
            {
                TotalAssignments = 0;
                Days = new PersonPerDayMemory[days];
                for (int i = 0; i < days; i++)
                    Days[i] = new PersonPerDayMemory();
            }
        }

        private struct DayMemory
        {
            public int? FirstAssignment;
            public int? LastAssignment;
            public int TotalAssignmentsFilled;
        }

        private readonly Dictionary<int, PersonMemory> _peopleMemory;
        private readonly DayMemory[] _daysMemory;
        private readonly int _daysInPartialSolution;
        private readonly PartialSolution _solution;

        public Operation(PartialSolution solution)
        {
            _peopleMemory = new();
            _daysInPartialSolution = solution.Days.Length;
            _daysMemory = new DayMemory[_daysInPartialSolution];
            _solution = solution;
        }

        private PersonMemory GetPersonMemory(int index)
        {
            if (_peopleMemory.TryGetValue(index, out var value))
                return value;
            return new PersonMemory(_daysInPartialSolution);
        }

        private void SavePersonMemory(int index, PersonMemory memory)
        {
            _peopleMemory[index] = memory;
        }

        public void Work()
        {
            for(int dIndex = 0; dIndex < _solution.Days.Length; dIndex++)
            {
                var solutionDay = _solution.Days[dIndex];
                for (var aIndex = 0; aIndex < solutionDay.SlotCount; aIndex++)
                {

                    for (int cIndex = 0; cIndex < solutionDay.Classrooms.Length; cIndex++)
                    {
                        // chronologically
                        var assignment = solutionDay.Classrooms[cIndex].Assignments[aIndex];

                        void WorkForPerson(int personId)
                        {
                            var memory = GetPersonMemory(personId);
                            memory.TotalAssignments++;
                            memory.Days[dIndex].FirstAssignment = memory.Days[dIndex].FirstAssignment ?? aIndex;
                            memory.Days[dIndex].LastAssignment = aIndex;
                            if(memory.Days[dIndex].AssignmentBlocks.Count == 0)
                            {
                                memory.Days[dIndex].AssignmentBlocks.Add(new PersonAssignmentBlock()
                                {
                                    ClassroomIndex = cIndex,
                                    StartIndex = aIndex,
                                    EndIndex = aIndex
                                });
                            }
                            else
                            {
                                var lastBlockIndex = memory.Days[dIndex].AssignmentBlocks.Count - 1;
                                var lastAssignmentBlock = memory.Days[dIndex].AssignmentBlocks[lastBlockIndex];
                                if(lastAssignmentBlock.EndIndex == aIndex - 1
                                    && lastAssignmentBlock.ClassroomIndex == cIndex)
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
                                        EndIndex = aIndex
                                    });
                                }
                                memory.Days[dIndex].TotalAssignments++;
                            }
                            SavePersonMemory(personId, memory);
                        }

                        if (assignment.IsSupervisorAndReviewerSet)
                        {
                            WorkForPerson(assignment.SupervisorId);
                            WorkForPerson(assignment.ReviewerId);
                        }
                        if (assignment.IsChairPersonSet)
                        {
                            WorkForPerson(assignment.ChairPersonId);
                        }

                        if(assignment.IsAllSet)
                        {
                            _daysMemory[dIndex].TotalAssignmentsFilled++;
                            _daysMemory[dIndex].FirstAssignment = _daysMemory[dIndex].FirstAssignment ?? aIndex;
                            _daysMemory[dIndex].LastAssignment = aIndex;
                        }
                    }
                }
            }
        }

        public decimal CalculateScore()
        {
            decimal sum = 0m;

            foreach (var (_, person) in _peopleMemory)
            {
                for (var dayIndex = 0; dayIndex < person.Days.Length; dayIndex++)
                {
                    var day = person.Days[dayIndex];

                    // if no assignments in this day, ignore
                    if (day.FirstAssignment == null || day.LastAssignment == null)
                    {
                        // prize for empty day multiplied by slot count
                        sum += 1.0m * _solution.Days[dayIndex].SlotCount;
                        continue;
                    }


                    var assignmentSpread = day.LastAssignment.Value - day.FirstAssignment.Value;
                    var assignmentsTotal = day.TotalAssignments; 
                    // min(min(0, x-a), min(0, b-x)), b > a
                    // penalty for having assignments away from a perfect range
                    // having one assignment in a day makes no sense
                    // having too many is overworking
                    sum += Math.Min(
                            Math.Min(0, assignmentsTotal - MinPerfectAssignmentsPerDayCount),
                            Math.Min(0, MaxPerfectAssignmentsPerDayCount - assignmentsTotal)
                            );

                    // starting early with one assignment, and finishing with another very late makes no sense
                    // limit how far those may be by giving negative points for the value being too big
                    sum += Math.Min(0, AcceptableMaxAssignmentSpreadPerDay - assignmentSpread);

                    for(int i = 0; i < day.AssignmentBlocks.Count; i++)
                    {
                        var assignmentBlock = day.AssignmentBlocks[i];
                        var assignmentsInBlock = (assignmentBlock.EndIndex - assignmentBlock.StartIndex) + 1;
                        // penalty for block being too large
                        sum += Math.Min(0, AcceptableMaxAssignmentsInBlockPerDay - assignmentSpread);

                        if (i < day.AssignmentBlocks.Count - 1)
                        {
                            // penalty for switching classes right between two assignments
                            var block2 = day.AssignmentBlocks[i + 1];
                            if (assignmentBlock.ClassroomIndex != block2.ClassroomIndex
                                && assignmentBlock.EndIndex + 1 == block2.StartIndex)
                            {
                                sum += -1;
                            }
                        }
                    }
                }
            }

            return sum;
        }
    }
}