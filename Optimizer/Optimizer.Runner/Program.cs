using System.Diagnostics;
using System.Globalization;
using CsvHelper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using OfficeOpenXml;
using Optimizer.Logic;
using Optimizer.Logic.Work;
using Optimizer.Runner;

ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddSimpleConsole(options =>
    {
        options.ColorBehavior = LoggerColorBehavior.Enabled;
        options.IncludeScopes = false;
        options.SingleLine = false;
        options.TimestampFormat = "HH:mm:ss.fff ";
    });
    builder.SetMinimumLevel(LogLevel.Trace);
});

var logger = loggerFactory.CreateLogger<Program>();

var solution = Imports.ImportData(
    new FileInfo("Input/Obrony.xlsx"),
    new FileInfo("Input/Przewodniczacy.xlsx"),
    new FileInfo("Input/Sale.xlsx"),
    new FileInfo("Input/Nieobecnosci.xlsx")
);

// var date = ConsoleHelper.GetDateTime();
var date = new DateOnly(2024, 1, 29);

var input = solution.GetOptimizerInput(date);

var ct = new CancellationTokenSource();

var algorithm = OptimizationAlgorithm.Optimize(input, ct.Token, loggerFactory);

var timeLimit = TimeSpan.FromSeconds(20);
var timeStart = DateTime.Now;
logger.LogInformation($"Start:  timeLimit: {timeLimit}");

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
        logger?.LogInformation("Timeout, cancelling...");
        return true;
    }

    return false;
}

Console.CancelKeyPress += (sender, eventArgs) => { Finish().RunSynchronously(); };

while (algorithm.IsWorking && !ShouldStopDueToTimeLimit())
{
    LogInfo();
    await Task.Delay(TimeSpan.FromSeconds(1));
}

await Finish();

async Task Finish()
{
    ct.Cancel();

    LogInfo();
    logger.LogInformation($"DONE! time: {DateTime.Now.Subtract(timeStart).TotalSeconds}");

    var exception = algorithm.GetThrownException();
    if (exception != null)
        logger.LogError(exception, "Optimize error");

    var result = algorithm.AssignmentOptimizerStateDetails?.Result;
    if (result != null)
    {
        var filename = "result-" + DateTime.Now.ToString("ddMMyy-HHmmss");
        var textResult = Exports.Pretty(result);
        logger.LogInformation(textResult);

        await using var writer = new StreamWriter($"{filename}.txt");
        await writer.WriteAsync(textResult);
        logger.LogInformation("Result saved: {Path}", Path.Join(Directory.GetCurrentDirectory(), $"{filename}.txt"));

        await Exports.WriteToXlsx(solution, $"{filename}.xlsx", result, overwrite: true);
        var path = Path.Join(Directory.GetCurrentDirectory(), $"{filename}.xlsx");
        logger.LogInformation("Result saved: {Path}", path);
        Process.Start("explorer", path);
    }
}

internal delegate bool EventHandler(CtrlType sig);

internal enum CtrlType
{
    CTRL_C_EVENT = 0,
    CTRL_BREAK_EVENT = 1,
    CTRL_CLOSE_EVENT = 2,
    CTRL_LOGOFF_EVENT = 5,
    CTRL_SHUTDOWN_EVENT = 6
}

public class Assignments
{
    public byte SupervisorId { get; set; }
    public byte ReviewerId { get; set; }
}

public class ChairPerson
{
    public byte ChairPersonId { get; set; }
}