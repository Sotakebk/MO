using Optimizer.Logic.Extensions;

namespace Optimizer.Logic.Work.ChairPersonOptimization.Scores;

internal class PreferenceAndEqualDistributionScore
{
    internal static float CalculateScore(OptimizerState state, TransformedInput tInput)
    {
        // minimize standard deviation of hours worked by people available as chairpersons
        return -state.ChairPersonWorkingAssignmentsAsAnyRoleCount.Values.StandardDeviation();
    }
}