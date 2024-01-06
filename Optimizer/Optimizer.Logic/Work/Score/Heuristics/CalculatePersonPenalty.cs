using Optimizer.Logic.Extensions;

namespace Optimizer.Logic.Work.Score.Heuristics;

class CalculatePersonPenalty : IHeuristic
{
    private decimal PenaltyOffsetFactor => new(1.0);
    private decimal PenaltyDayChangeScore => new(30.0); // day change is like 8 gaps in day
    private decimal PenaltyEqualityUsageScore => new(20.0);
    private decimal PenaltyMinimalChairPersons => new(2.0);
    private decimal PenaltySubsequentExceed => new(10.0);
    private int NSubsequent => 9; // day change is like 8 gaps in day

    public decimal CalculateScore(PartialSolution solution)
    {
        // TODO3: Żeby obrony zaczynały się od rana (czyli mozna liczyc ile jest dziury rano mają mniejsze penalty, niż dziury wieczorem), użycie kolejnego dnia obron powinno być również penalty
        // TODO4: Plusik za wypełnienie promotora_recenzenta i supervisora 

        var chairpersonSet = new Dictionary<int, float>();
        var personLastAppearance = new Dictionary<int, int?>();
        var personLastSubsequentStart = new Dictionary<int, int?>();
        var daysCount = 0;
        var assignmentsCount = 0;

        var subsequentPenalty = new decimal(0.0);
        var dayChangedPenalty = new decimal(0.0);
        var offsetsPenalty = new decimal(0.0);

        foreach (var day in solution.Days)
        {
            daysCount += 1;
            foreach (var block in day.Blocks)
            {
                for (var i = 0; i < block.Assignments.Length; i++)
                {
                    assignmentsCount += 1;
                    var assignment = block.Assignments[i];
                    var offset = i - block.Offset;

                    if (assignment.IsChairPersonSet)
                    {
                        personLastAppearance.AddOrUpdate(assignment.ChairPersonId, offset, (key, lastOffset) => UpdatePersonOffsetUpdatePenalty(key, lastOffset, offset));
                        chairpersonSet.AddOrUpdate(assignment.ChairPersonId, 1, last => last + 1);
                    }

                    if (assignment.IsSupervisorAndReviewerSet)
                    {
                        personLastAppearance.AddOrUpdate(assignment.ReviewerId, offset, (key, lastOffset) => UpdatePersonOffsetUpdatePenalty(key, lastOffset, offset));
                        personLastAppearance.AddOrUpdate(assignment.SupervisorId, offset, (key, lastOffset) => UpdatePersonOffsetUpdatePenalty(key, lastOffset, offset));
                    }
                }
            }

            // null all persons
            foreach (var key in personLastAppearance.Keys)
            {
                personLastAppearance.AddOrUpdate(key, null, _ => null);
            }
        }

        // check subsequent usage if no used again
        foreach (var subsequentOffset in personLastSubsequentStart.Values)
        {
            if (subsequentOffset.HasValue)
                subsequentPenalty += CalcSubsequentPenalty(subsequentOffset.Value);
        }

        // sprawdzenie czy uzyci chairpersoni sa w miare rowno wykorzystywani
        var equalChairPersonUsagePenalty = chairpersonSet.Values.Select(number => number).StandardDeviation() * PenaltyEqualityUsageScore;

        // sprawdzenie czy nie uzywamy za duzo chairpersonow (wiecej niz potrzeba)
        var chairPersonUsagePenalty = new decimal(1.0) * (chairpersonSet.Count - assignmentsCount) / daysCount / NSubsequent * PenaltyMinimalChairPersons;

        return -(subsequentPenalty + dayChangedPenalty + offsetsPenalty + equalChairPersonUsagePenalty + chairPersonUsagePenalty);

        int? UpdatePersonOffsetUpdatePenalty(int key, int? lastOffset, int offset)
        {
            if (lastOffset == null) // null is set at the end of the day, so this is the first appearance on the next day
            {
                dayChangedPenalty += PenaltyDayChangeScore;
                personLastSubsequentStart.AddOrUpdate(key, lastOffset, ResetSubsequentCount);
            }
            else if (lastOffset.Value - offset - 1 > 0) // check if the gap is higher than 1
            {
                offsetsPenalty += (lastOffset.Value - offset - 1) * PenaltyOffsetFactor;
                personLastSubsequentStart.AddOrUpdate(key, lastOffset, ResetSubsequentCount);
            }
            else // subsequent
            {
                personLastSubsequentStart.AddOrUpdate(key, lastOffset, AddSubsequentCount);
            }

            int? ResetSubsequentCount(int? value)
            {
                if (value.HasValue)
                    subsequentPenalty += CalcSubsequentPenalty(value.Value);
                return null;
            }

            int? AddSubsequentCount(int? value) => value.HasValue ? value.Value + 1 : 1;


            return offset;
        }
    }


    private decimal CalcSubsequentPenalty(int subsequentCount)
    {
        var exceed = Math.Max(subsequentCount - NSubsequent, 0);
        return exceed * exceed * PenaltySubsequentExceed;
    }
}