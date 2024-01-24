namespace Optimizer.Runner;

public static class ConsoleHelper
{
    public static DateOnly GetDateTime()
    {
        DateOnly dt;
        string? line = null;
        while (!DateOnly.TryParseExact(line, "dd-MM-yyyy", out dt))
        {
            Console.WriteLine("Podaj datę rozpoczęcia obron w formacie: dd-mm-yyyy, przykładowo: '15-01-2023'");
            line = Console.ReadLine();
        }

        return dt;
    }
}