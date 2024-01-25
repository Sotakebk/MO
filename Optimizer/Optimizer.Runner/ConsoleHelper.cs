using Spectre.Console;

namespace Optimizer.Runner;

public static class ConsoleHelper
{
    public static DateOnly GetDateTime()
    {
        DateOnly dt;
        string? line = null;
        while (!DateOnly.TryParseExact(line, "dd-MM-yyyy", out dt))
        {
            line = AnsiConsole.Ask<string>("Podaj datę rozpoczęcia obron w formacie: dd-mm-yyyy, przykładowo: '15-01-2023'");
        }

        return dt;
    }
}