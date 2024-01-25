namespace Optimizer.Logic.Work.AssignmentOptimization.Rules;

internal static class SingleAssignmentRule
{
    internal static bool PassesRule(OptimizerAction action, OptimizerState state, TransformedInput tInput)
    {
        var day = state.Days[action.SlotId.Day];

        for (var i = 0; i < day.Classrooms.Length; i++)
        {
            // check collision against preference A
            if (tInput
                    .Days[action.SlotId.Day]
                    .Classrooms[i]
                    .Slots[action.SlotId.Slot]
                    .Preferences[action.A] == PreferenceType.NotAllowed)
                return false;

            // check collision against preference B
            if (tInput
                    .Days[action.SlotId.Day]
                    .Classrooms[i]
                    .Slots[action.SlotId.Slot]
                    .Preferences[action.B] == PreferenceType.NotAllowed)
                return false;

            // check collision against chairperson
            var classroom = tInput.Days[action.SlotId.Day].Classrooms[i];
            if (classroom.Slots.Length > action.SlotId.Slot) // this slot exists in the classroom
            {
                var chairPersonId = classroom
                    .Slots[action.SlotId.Slot]
                    .ChairPersonId;

                if (action.A == chairPersonId || action.B == chairPersonId)
                    return false;

                // check collision against what is assigned
                var assignment = day.Classrooms[i].Slots[action.SlotId.Slot];
                if (assignment.HasValuesSet() && HasCollision(action, assignment))
                    return false;
            }
        }

        return true;
    }

    private static bool HasCollision(OptimizerAction action, Slot slot)
    {
        var a1 = action.A;
        var b1 = action.B;

        var a2 = slot.A;
        var b2 = slot.B;

        return a1 == a2 || a1 == b2
                        || b1 == a2 || b1 == b2;
    }
}