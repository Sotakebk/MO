using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using OfficeOpenXml;
using Optimizer.Logic;
using Optimizer.Logic.Work;
using Optimizer.Runner;
using Spectre.Console;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;
ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

using var loggerFactory = LoggerFactory.Create(builder =>
{
#if DEBUG
    builder.AddSimpleConsole(options =>
    {
        options.ColorBehavior = LoggerColorBehavior.Enabled;
        options.IncludeScopes = false;
        options.SingleLine = false;
        options.TimestampFormat = "HH:mm:ss.fff ";
    });
#endif
    builder.AddFile("app.log");
    builder.SetMinimumLevel(LogLevel.Error);
});

var logger = loggerFactory.CreateLogger<Program>();

AnsiConsole.Write(new FigletText("PK Optim").LeftJustified().Color(Color.Aquamarine1));

// Simulate some work
AnsiConsole.MarkupLine("Ładowanie plików");

var solution = Imports.ImportData(
    new FileInfo("Input/Obrony.xlsx"),
    new FileInfo("Input/Przewodniczacy.xlsx"),
    new FileInfo("Input/Sale.xlsx"),
    new FileInfo("Input/Nieobecnosci.xlsx")
);

AnsiConsole.MarkupLine("Tworzenie danych wejściowych do algorytmu");
var date = ConsoleHelper.GetDateTime();
// var date = new DateOnly(2024, 1, 29);
var input = solution.GetOptimizerInput(date);

var timeLimit = TimeSpan.FromSeconds(20);

AnsiConsole.MarkupLine($"Algorytm zakończy swoją pracę za: [bold]{timeLimit.TotalSeconds}sekund[/]");
AnsiConsole.MarkupLine("Wcześniejsze wyłączenie po kliknięciu: [bold]CTRL+C[/]");
AnsiConsole.MarkupLine("Jeżeli do tego czasu algorytm nie znalazł rozwiązania, nic nie zostanie zapisane");
logger.LogInformation("Start:  timeLimit: {TimeLimit}", timeLimit);


await AnsiConsole
    .Status()
    .StartAsync("Uruchamianie....", async ctx =>
    {
        var ct = new CancellationTokenSource();
        var algorithm = OptimizationAlgorithm.Optimize(input, ct.Token, loggerFactory);
        var timeStart = DateTime.Now;

        ctx.Status("Wyszukiwanie rozwiązania");
        ctx.Spinner(Spinner.Known.Material);

        Console.CancelKeyPress += (_, _) => { Finish().RunSynchronously(); };

        algorithm.OnBetterSolutionFound += (_, res) => { AnsiConsole.MarkupLine($"Znaleziono nowe rozwiązanie. [italic]{res.Score}[/]"); };

        while (algorithm.IsWorking && !ShouldStopDueToTimeLimit())
        {
            LogInfo();
            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        await Finish();

        IOptimizerStateDetails? GetActiveOptimizerStateDetails()
        {
            if (algorithm?.AssignmentOptimizerStateDetails != null)
                return algorithm.AssignmentOptimizerStateDetails;

            if (algorithm?.ChairPersonOptimizerStateDetails != null)
                return algorithm.ChairPersonOptimizerStateDetails;
            return null;
        }

        void LogInfo()
        {
            var state = GetActiveOptimizerStateDetails();
            logger.LogInformation(
                $"ops: {state?.OperationsDone:D10}, evs: {state?.Evaluations}, dead-ends: {state?.DeadEnds}, partial score: {state?.PartialScore}, best score:{state?.ResultScore:F5}, depth: {state?.CurrentDepth} ({(100f * state?.CurrentDepth / state?.MaxDepth):F}%, level: {(100 * state?.CurrentDepthCompleteness):F}%), pds: {state?.PercentDomainSeen:F5}%");
        }

        bool ShouldStopDueToTimeLimit()
        {
            if (DateTime.Now > timeStart.Add(timeLimit))
            {
                AnsiConsole.MarkupLine("Czas minął.");
                logger?.LogInformation("Timeout, cancelling...");
                return true;
            }

            return false;
        }

        async Task Finish()
        {
            AnsiConsole.MarkupLine("Optymalizacja zakończona");
            ct.Cancel();
            LogInfo();
            logger.LogInformation("DONE! time: {TotalSeconds}", DateTime.Now.Subtract(timeStart).TotalSeconds);

            var exception = algorithm.GetThrownException();
            if (exception != null)
                logger.LogError(exception, "Optimize error");

            var result = algorithm.AssignmentOptimizerStateDetails?.Result;
            if (result != null)
            {
                ctx.Status("Zapisywanie wyników");
                var filename = "Plan Obron-" + DateTime.Now.ToString("HHmmss-ddMMyy");
#if DEBUG
                await Exports.WriteToXlsx(solution, $"{filename}.xlsx", result, overwrite: true, true);
#else
                await Exports.WriteToXlsx(solution, $"{filename}.xlsx", result, overwrite: true);
#endif
                var path = Path.Join(Directory.GetCurrentDirectory(), $"{filename}.xlsx");
                logger.LogInformation("Result saved: {Path}", path);
                Process.Start("explorer", path);
            }
            else
            {
                AnsiConsole.MarkupLine("Nie znaleziono rozwiązania");
            }
        }
    });


AnsiConsole.MarkupLine("Dziękujemy za wspólną podróż!");

AnsiConsole.WriteLine();

var root = new Tree("Credits");
root.AddNode("[aqua]Aleksander Rucki[/]");
root.AddNode("[blue]Jan Bartula[/]");
root.AddNode("[green]Dawid Banach[/]");
root.AddNode("[cyan]Jan Maślanka[/]");
AnsiConsole.Write(root);

AnsiConsole.WriteLine();

AnsiConsole.Write(new BreakdownChart()
    .Width(60)
    .AddItem("Serca", 70, Color.Red)
    .AddItem("Zarwanych nocy", 15, Color.Aqua)
    .AddItem("Szczęścia", 10, Color.Blue)
    .AddItem("Rozumu", 5, Color.Green)
);

AnsiConsole.WriteLine();

AnsiConsole.Ask("Naciśnij dowolny klawisz aby zakończyć program.", true);