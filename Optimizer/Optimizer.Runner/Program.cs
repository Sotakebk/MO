using System.Diagnostics;
using System.Globalization;
using CsvHelper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using OfficeOpenXml;
using Optimizer.Logic;
using Optimizer.Runner;

ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

using var reader = new StreamReader(Path.Combine("Examples", "assignments.csv"));
using var reader2 = new StreamReader(Path.Combine("Examples", "chairpersons.csv"));
using var csv1 = new CsvReader(reader, CultureInfo.InvariantCulture);
using var csv2 = new CsvReader(reader2, CultureInfo.InvariantCulture);
var records1 = csv1.GetRecords<Assignments>();
var records2 = csv2.GetRecords<ChairPerson>();

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

var root = new Root(loggerFactory);

Input input = new Input()
{
    ChairPersonIds = records2.Select(row => row.ChairPersonId).ToArray(),
    Combinations = records1
        .Select(assignments => assignments.ReviewerId < assignments.SupervisorId ? (assignments.ReviewerId, assignments.SupervisorId) : (assignments.SupervisorId, assignments.ReviewerId))
        .GroupBy(assignments => (assignments.Item1, assignments.Item2),
            (key, enumerable) => new InputCombination()
            {
                ReviewerId = key.Item2,
                PromoterId = key.Item1,
                TotalCount = enumerable.Count()
            }).ToArray(),
    Days = new[]
    {
        new InputDay()
        {
            Id = 1,
            Classrooms = new[]
            {
                new InputClassroom() { RoomId = 1 },
                new InputClassroom() { RoomId = 2 }
            },
            SlotCount = 18
        },
        new InputDay()
        {
            Id = 2,
            Classrooms = new[]
            {
                new InputClassroom() { RoomId = 1 },
                new InputClassroom() { RoomId = 2 }
            },
            SlotCount = 18
        },
    },
    ForbiddenSlots = Array.Empty<(int, int, int)>()
};

var ct = new CancellationTokenSource();

var state = root.Optimize(input, ct.Token, OptimizerType.Simple);

var operationsLimit = 100000000;
var timeLimit = TimeSpan.FromSeconds(60);

var timeStart = DateTime.Now;

logger.LogInformation($"Start: operationsLimit: {operationsLimit}, timeLimit: {timeLimit}");

void LogInfo()
{
    logger.LogInformation($"Operations: {state.OperationsDone:D10}, evaluations: {state.Evaluations}, dead-ends: {state.DeadEnds}, partial score: {state.PartialScore}, best complete score:{state.Result?.Score:F5}, depth: {state.CurrentDepth} ({(100f * state.CurrentDepth / (float)state.MaxDepth):F}%, level: {(100 * state.CurrentDepthCompleteness):F}%), pds: {state.PercentDomainSeen:F5}%");
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

Console.CancelKeyPress += (sender, eventArgs) =>
{
    Finish().RunSynchronously();
};

while (state.IsWorking && !ShouldStopDueToTimeLimit())
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

    if (state.Task?.Exception != null)
        logger.LogError(state.Task?.Exception, "Optimize error");

    if (state.Result.HasValue)
    {
        var filename = "result-" + DateTime.Now.ToString("ddMMyy-HHmmss");
        var textResult = Exports.Pretty(state.Result.Value);
        logger.LogInformation(textResult);

        await using var writer = new StreamWriter($"{filename}.txt");
        await writer.WriteAsync(textResult);
        logger.LogInformation("Result saved: {Path}", Path.Join(Directory.GetCurrentDirectory(), $"{filename}.txt"));

        await Exports.WriteToXlsx($"{filename}.xlsx", state.Result.Value, overwrite: true);
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
    public int SupervisorId { get; set; }
    public int ReviewerId { get; set; }
}

public class ChairPerson
{
    public int ChairPersonId { get; set; }
}