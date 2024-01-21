namespace Optimizer.Logic.Extensions;

public static class EnumeratorExtensions
{
    public static float StandardDeviation(this IEnumerable<float> values)
    {
        var list = values.ToArray();
        if (list.Length == 0)
            return 0;
        var avg = list.Average();
        return (float)(Math.Sqrt(list.Average(v => (v - avg) * (v - avg))));
    }
    public static float StandardDeviation(this IEnumerable<int> values)
    {
        var list = values.ToArray();
        if (list.Length == 0)
            return 0;
        var avg = list.Average();
        return (float)(Math.Sqrt(list.Average(v => (v - avg) * (v - avg))));
    }
}