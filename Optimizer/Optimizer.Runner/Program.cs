// See https://aka.ms/new-console-template for more information

using System.Globalization;
using System.Text.Json;
using CsvHelper;
using Microsoft.Extensions.Logging;
using Optimizer.Logic;

// ReSharper disable TemplateIsNotCompileTimeConstantProblem

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
            Id = 1, VacantBlocks = new[]
            {
                new InputVacantBlock() { Offset = 0, RoomId = 1, SlotCount = 18 },
                new InputVacantBlock() { Offset = 0, RoomId = 2, SlotCount = 18 }
            }
        },
        new InputDay()
        {
            Id = 2, VacantBlocks = new[]
            {
                new InputVacantBlock() { Offset = 0, RoomId = 1, SlotCount = 18 },
                new InputVacantBlock() { Offset = 0, RoomId = 2, SlotCount = 18 }
            }
        },
    }
};

var ct = new CancellationTokenSource();

var state = root.Optimize(input, ct.Token);


var operationsLimit = 10000000;
var timeLimit = TimeSpan.FromSeconds(60);


var timeStart = DateTime.Now;


logger.LogInformation($"Start: operationsLimit:{operationsLimit}, timeLimit: {timeLimit}");

while (state.IsWorking && state.OperationsDone < operationsLimit && DateTime.Now < timeStart.Add(timeLimit))
{
    logger.LogInformation($"IT: operations: {state.OperationsDone}, score:{state.Result?.Score}, depth:{state.CurrentDepth}");
    await Task.Delay(TimeSpan.FromSeconds(1));
}

ct.Cancel();

logger.LogInformation($"DONE: Iter: {state.OperationsDone}, Score: {state.Result?.Score}, Depth: {state.CurrentDepth}, IN: {DateTime.Now.Subtract(timeStart).TotalSeconds}s");

if (state.Task?.Exception != null)
    logger.LogError(state.Task?.Exception, "Optimize error");


if (state.Result.HasValue)
{
    await using var writer = new StreamWriter("result.json");
    await writer.WriteAsync(JsonSerializer.Serialize(state.Result.Value));
    logger.LogInformation("Result saved: {Path}", Path.Join(Directory.GetCurrentDirectory(), "result.json"));
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