// ReSharper disable TemplateIsNotCompileTimeConstantProblem

using System.Globalization;
using CsvHelper;
using Microsoft.Extensions.Logging;
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
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Trace);
});
var logger = loggerFactory.CreateLogger<Program>();
logger.LogInformation("START OF THIS MASTERPIECE");

var root = new Root(loggerFactory);


Input input = new Input()
{
    ChairPersonIds = records2.Select(row => row.ChairPersonId).ToArray(),
    Combinations = records1
        .GroupBy(assignments => (assignments.ReviewerId, assignments.SupervisorId),
            (key, enumerable) => new InputCombination()
            {
                ReviewerId = key.ReviewerId,
                PromoterId = key.SupervisorId,
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
var timeLimit = TimeSpan.FromSeconds(30) * 2 * 10;


var timeStart = DateTime.Now;


logger.LogInformation($"Start: operationsLimit:{operationsLimit}, timeLimit: {timeLimit}");

while (state.IsWorking && state.OperationsDone < operationsLimit && DateTime.Now < timeStart.Add(timeLimit))
{
    logger.LogInformation($"IT: operations: {state.OperationsDone:D10}, dead-ends: {state.DeadEnds}, score:{state.Result?.Score:F5}, depth:{state.CurrentDepth}");
    await Task.Delay(TimeSpan.FromSeconds(1));
}

ct.Cancel();

logger.LogInformation($"DONE: Iterations: {state.OperationsDone}, Score: {state.Result?.Score}, Depth: {state.CurrentDepth}, IN: {DateTime.Now.Subtract(timeStart).TotalSeconds}s");

if (state.Task?.Exception != null)
    logger.LogError(state.Task?.Exception, "Optimize error");


if (state.Result.HasValue)
{
    var textResult = Exports.Pretty(state.Result.Value);
    logger.LogInformation(textResult);

    await using var writer = new StreamWriter("result.txt");
    await writer.WriteAsync(textResult);
    logger.LogInformation("Result saved: {Path}", Path.Join(Directory.GetCurrentDirectory(), "result.txt"));

    await Exports.WriteToXlsx("result.xlsx", state.Result.Value, overwrite: true);
    logger.LogInformation("Result saved: {Path}", Path.Join(Directory.GetCurrentDirectory(), "result.xlsx"));
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