namespace Optimizer.Logic.Work.ChairPersonOptimization.Rules;

internal static class NoCollisionRule
{
    internal static bool PassesRule(OptimizerAction action, OptimizerState state, TransformedInput tInput)
    {
        
        if (tInput
                .Days[action.Position.Day]
                .Classrooms[action.Position.Classroom]
                .Blocks[action.Position.Block]
                .ChairPeoplePreferences[action.ChairPersonId]
                .NotAllowedCount > 0)
            return false;
        
        if (!state.ChairPersonWorkingAssignmentsAsChairPersonCounts.TryGetValue(action.ChairPersonId, out _))
            return true; // did not get assigned yet (fast lookup)

        var sDay = state.Days[action.Position.Day];
        var tDay = tInput.Days[action.Position.Day];


        var tBlock = tDay.Classrooms[action.Position.Classroom].Blocks[action.Position.Block];
        var (x1, x2) = (Start: tBlock.First, End: tBlock.Last);

        for (var c = 0; c < sDay.Classrooms.Length; c++)
        {
            var sClassroom = state.Days[action.Position.Day].Classrooms[c];
            var tClassroom = tInput.Days[action.Position.Day].Classrooms[c];
            for (var b = 0; b < sClassroom.Blocks.Length; b++)
            {
                var tOtherBlock = tClassroom.Blocks[b];
                var (y1, y2) = (Start: tOtherBlock.First, End: tOtherBlock.Last);
                if (x1 <= y2 && y1 <= x2)
                {
                    // is actually a potential collision
                    var sOtherBlock = sClassroom.Blocks[b];
                    if (sOtherBlock.IsAssigned && sOtherBlock.ChairPersonId == action.ChairPersonId)
                        return false;
                }
            }
        }

        return true;
    }
}