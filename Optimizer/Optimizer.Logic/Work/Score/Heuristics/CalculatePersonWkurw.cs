using Optimizer.Logic.Extensions;

namespace Optimizer.Logic.Work.Score.Heuristics;

class CalculatePersonWkurw : IHeuristic
{
    private decimal WkurwOffsetFactor => new(1.0);
    private decimal WkurwDayChangeScore => new(8.0); // day change is like 8 gaps in day

    public decimal CalculateScore(PartialSolution solution)
    {
        Dictionary<int, int?> personLastAppear = new Dictionary<int, int?>();
        var wkurw = new decimal(0.0);


        foreach (var day in solution.Days)
        {
            foreach (var block in day.Blocks)
            {
                for (var i = 0; i < block.Assignments.Length; i++)
                {
                    var assignment = block.Assignments[i];
                    var offset = i - block.Offset;

                    personLastAppear.AddOrUpdate(assignment.ChairPersonId, offset, val =>
                    {
                        if (val == null)
                        {
                            // null is set at end of day, so this is first appearing on next day
                            wkurw += WkurwDayChangeScore;
                        }
                        else if (val.Value - offset - 1 > 0) // check if gap is higher than 1
                            wkurw += (val.Value - offset - 1) * WkurwOffsetFactor;

                        return offset;
                    });
                }
            }


            // null all persons
            foreach (var key in personLastAppear.Keys)
            {
                personLastAppear.AddOrUpdate(key, null, _ => null);
            }
        }

        return -wkurw;
    }
}