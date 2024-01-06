using Optimizer.Logic.Extensions;

namespace Optimizer.Logic.Work.Score.Heuristics;

class CalculatePersonPenalty : IHeuristic
{
    private decimal PenaltyOffsetFactor => new(1.0);
    private decimal PenaltyDayChangeScore => new(8.0); // day change is like 8 gaps in day

    public decimal CalculateScore(PartialSolution solution)
    {
        // TODO: Add penalty for more than N subsequent assignments
        // TODO: Add penalty for using only one chairperson (srednai wystapien danego chairpersona powinna byc podobna)
        // TODO: Żeby obrony zaczynały się od rana (czyli mozna liczyc ile jest dziury rano mają mniejsze penalty, niż dziury wieczorem), użycie kolejnego dnia obron powinno być również penalty

        var personLastAppearance = new Dictionary<int, int?>();
        var penalty = new decimal(0.0);
        var maxSubsequentAssignments = 3;

        foreach (var day in solution.Days)
        {
            foreach (var block in day.Blocks)
            {
                for (var i = 0; i < block.Assignments.Length; i++)
                {
                    var assignment = block.Assignments[i];
                    var offset = i - block.Offset;

                    if (assignment.IsChairPersonSet)
                        personLastAppearance.AddOrUpdate(assignment.ChairPersonId, offset, lastOffset => UpdatePersonOffsetUpdatePenalty(lastOffset, offset));

                    if (assignment.IsSupervisorAndReviewerSet)
                    {
                        personLastAppearance.AddOrUpdate(assignment.ReviewerId, offset, lastOffset => UpdatePersonOffsetUpdatePenalty(lastOffset, offset));
                        personLastAppearance.AddOrUpdate(assignment.SupervisorId, offset, lastOffset => UpdatePersonOffsetUpdatePenalty(lastOffset, offset));
                    }
                }
            }

            // null all persons
            foreach (var key in personLastAppearance.Keys)
            {
                personLastAppearance.AddOrUpdate(key, null, _ => null);
            }
        }

        int? UpdatePersonOffsetUpdatePenalty(int? lastOffset, int offset)
        {
            if (lastOffset == null) // null is set at end of day, so this is first appearing on next day
                penalty += PenaltyDayChangeScore;
            else if (lastOffset.Value - offset - 1 > 0) // check if gap is higher than 1
                penalty += (lastOffset.Value - offset - 1) * PenaltyOffsetFactor;

            return offset;
        }

        return -penalty;
    }
}