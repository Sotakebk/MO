namespace Optimizer.Logic.Extensions;

public static class EnumeratorExtensions
{
    public static decimal StandardDeviation(this IEnumerable<float> values)
    {
        var list = values.ToArray();
        if (list.Length == 0)
            return 0;
        var avg = list.Average();
        return new decimal(Math.Sqrt(list.Average(v => (v - avg) * (v - avg))));
    }
}