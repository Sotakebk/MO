namespace Optimizer.Runner;

public class TimePeriod
{
    public TimeOnly StartTime { get; }
    public TimeOnly EndTime { get; }

    public TimePeriod(TimeOnly startTime, TimeOnly endTime)
    {
        if (endTime <= startTime)
        {
            throw new ArgumentException("End time must be greater than start time.");
        }

        StartTime = startTime;
        EndTime = endTime;
    }

    public bool IsInPeriod(TimeOnly time)
    {
        return time >= StartTime && time < EndTime;
    }

    public static TimePeriod Parse(string input)
    {
        var parts = input.Split('-');
        if (parts.Length == 2 && TimeOnly.TryParse(parts[0].Trim(), out var startTime) && TimeOnly.TryParse(parts[1].Trim(), out var endTime))
        {
            return new TimePeriod(startTime, endTime);
        }

        throw new UserFriendlyException("Could not parse format", $"Nie udało się odczytać przedziału czasu: {input}, sprawdź format");
    }
}