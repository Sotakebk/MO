using System.Text;
using OfficeOpenXml;
using Optimizer.Logic;

namespace Optimizer.Runner;

public static class Exports
{
    public static async Task WriteToXlsx(string fileName, Logic.Work.AssignmentOptimization.OptimizerOutput solution, bool overwrite = true)
    {
        var colorPalette = Colors.GenerateColorPalette(30);

        if (overwrite && File.Exists(fileName))
            File.Delete(fileName);
        using var p = new ExcelPackage(fileName);
        var ws = p.Workbook.Worksheets.Add("Schedule");
        var currentRow = 1;
        for (var dayIndex = 0; dayIndex < solution.Days.Length; dayIndex++)
        {
            var day = solution.Days[dayIndex];
            ws.Cells[currentRow, 1, currentRow, 3 * day.Classrooms.Length].Merge = true;
            ws.Cells[currentRow, 1].Value = $"Day {dayIndex + 1}";
            currentRow++;


            var maxRows = 0;

            for (var roomIndex = 0; roomIndex < day.Classrooms.Length; roomIndex++)
            {
                var subCurrentRow = 0;
                var classroom = day.Classrooms[roomIndex];
                ws.Cells[currentRow + subCurrentRow, roomIndex * 3 + 1, currentRow, roomIndex * 3 + 3].Merge = true;
                ws.Cells[currentRow + subCurrentRow, roomIndex * 3 + 1].Value = $"Room {roomIndex + 1}";
                subCurrentRow++;

                foreach (var assignment in classroom.Slots)
                {
                    ws.Cells[currentRow + subCurrentRow, roomIndex * 3 + 1].Value = assignment.A?.ToString();
                    if (assignment.A.HasValue)
                        ws.Cells[currentRow + subCurrentRow, roomIndex * 3 + 1].Style.Fill.SetBackground(colorPalette[assignment.A.Value]);

                    ws.Cells[currentRow + subCurrentRow, roomIndex * 3 + 2].Value = assignment.B?.ToString();

                    if (assignment.B.HasValue)
                        ws.Cells[currentRow + subCurrentRow, roomIndex * 3 + 2].Style.Fill.SetBackground(colorPalette[assignment.B.Value]);

                    ws.Cells[currentRow + subCurrentRow, roomIndex * 3 + 3].Value = assignment.ChairPersonId;
                    ws.Cells[currentRow + subCurrentRow, roomIndex * 3 + 3].Style.Fill.SetBackground(colorPalette[assignment.ChairPersonId]);
                    subCurrentRow++;
                }

                maxRows = maxRows < subCurrentRow ? subCurrentRow : maxRows;
            }

            currentRow += maxRows;
            currentRow++;
        }

        await p.SaveAsync();
    }

    public static string Pretty(Logic.Work.AssignmentOptimization.OptimizerOutput solution)
    {
        var sb = new StringBuilder();
        foreach (var day in solution.Days)
        {
            sb.AppendLine($"=== Day {day.Id} ===");
            foreach (var classroom in day.Classrooms)
            {
                sb.AppendLine($"== Classroom {classroom.Id} ==");
                foreach (var assignment in classroom.Slots)
                    sb.AppendLine(assignment.ToString());
            }

            sb.AppendLine("");
        }

        return sb.ToString();
    }
}