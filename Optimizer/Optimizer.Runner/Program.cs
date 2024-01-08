// See https://aka.ms/new-console-template for more information

using System.Globalization;
using System.Text;
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

var state = root.Optimize(input, ct.Token);


var operationsLimit = 10000000;
var timeLimit = TimeSpan.FromSeconds(60);


var timeStart = DateTime.Now;


logger.LogInformation($"Start: operationsLimit:{operationsLimit}, timeLimit: {timeLimit}");

while (state.IsWorking && state.OperationsDone < operationsLimit && DateTime.Now < timeStart.Add(timeLimit))
{
    logger.LogInformation($"IT: operations: {state.OperationsDone:D10}, score:{state.Result?.Score:F5}, depth:{state.CurrentDepth}");
    await Task.Delay(TimeSpan.FromSeconds(1));
}

ct.Cancel();

logger.LogInformation($"DONE: Iter: {state.OperationsDone}, Score: {state.Result?.Score}, Depth: {state.CurrentDepth}, IN: {DateTime.Now.Subtract(timeStart).TotalSeconds}s");

if (state.Task?.Exception != null)
    logger.LogError(state.Task?.Exception, "Optimize error");


if (state.Result.HasValue)
{
    var sb = new StringBuilder();
    foreach(var day in state.Result.Value.Days)
    {
        sb.AppendLine($"=== Day {day.DayId} ===");
        foreach (var classroom in day.Classrooms)
        {
            sb.AppendLine($"== Classroom {classroom.RoomId} ==");
            foreach (var assignment in classroom.Assignments)
                sb.AppendLine(assignment.ToString());
        }
        sb.AppendLine("");
    }

    logger.LogInformation(sb.ToString());

    await using var writer = new StreamWriter("result.txt");
    await writer.WriteAsync(sb.ToString()); //JsonSerializer.Serialize(state.Result.Value)
    logger.LogInformation("Result saved: {Path}", Path.Join(Directory.GetCurrentDirectory(), "result.json"));

    //await using var writerCsv = new StreamWriter("result.json");
    //state.Result.Value.Days.Select(d => d.Classrooms.Select(c => c.Assignments.Select(a => (a.Value.ReviewerId, a.Value.SupervisorId, a.Value.ChairPersonId)))).ToList();

    var records = new List<CsvRow>
    {
        new CsvRow { DayId = 0, ClassroomId= 0, ChairPersonId=0, ReviewerId=0, SupervisorId=0 },
    };

    using (var writerCsv = new StreamWriter(Path.Join(Directory.GetCurrentDirectory(), "result.csv")))
    using (var csv = new CsvWriter(writerCsv, CultureInfo.InvariantCulture))
    {
        csv.WriteRecords(records);
    }

}


public class CsvRow
{
    public int DayId{ get; set; }
    public int ClassroomId{ get; set; }
    public int? ChairPersonId { get; set; }
    public int? SupervisorId { get; set; }
    public int? ReviewerId { get; set; }
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