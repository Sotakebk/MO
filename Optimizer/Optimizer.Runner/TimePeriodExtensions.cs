namespace Optimizer.Runner;

public static class TimePeriodExtensions
{
    public static IEnumerable<int> SlotIndices(this ICollection<TimePeriod> settingValue)
    {
        foreach (var timePeriod in settingValue)
        {
            var index = (int)Math.Floor((timePeriod.StartTime - new TimeOnly(8, 0, 0)).Minutes / 30f);

            index = Math.Max(index, 0);

            var current = new TimeOnly(8, 0, 0).AddMinutes(30 * index);
            while (current < timePeriod.EndTime)
            {
                if (current <= timePeriod.EndTime && timePeriod.StartTime < current.AddMinutes(30))
                    yield return index;
                index++;
                current = current.AddMinutes(30);
            }
        }
    }
}