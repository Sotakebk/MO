using System.Text;
using OfficeOpenXml;
using Optimizer.Logic;

namespace Optimizer.Runner;

public static class Exports
{
    public static async Task WriteToXlsx(string fileName, Solution solution, bool overwrite = true)
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

                foreach (var assignment in classroom.Assignments)
                {
                    ws.Cells[currentRow + subCurrentRow, roomIndex * 3 + 1].Value = assignment.ChairPersonId;
                    ws.Cells[currentRow + subCurrentRow, roomIndex * 3 + 1].Style.Fill.SetBackground(colorPalette[assignment.ChairPersonId]);

                    ws.Cells[currentRow + subCurrentRow, roomIndex * 3 + 2].Value = assignment.SupervisorId;
                    ws.Cells[currentRow + subCurrentRow, roomIndex * 3 + 2].Style.Fill.SetBackground(colorPalette[assignment.SupervisorId]);

                    ws.Cells[currentRow + subCurrentRow, roomIndex * 3 + 3].Value = assignment.ReviewerId;
                    ws.Cells[currentRow + subCurrentRow, roomIndex * 3 + 3].Style.Fill.SetBackground(colorPalette[assignment.ReviewerId]);
                    subCurrentRow++;
                }

                maxRows = maxRows < subCurrentRow ? subCurrentRow : maxRows;
            }

            currentRow += maxRows;
            currentRow++;
        }

        await p.SaveAsync();
    }

    public static string Pretty(Solution solution)
    {
        var sb = new StringBuilder();
        foreach (var day in solution.Days)
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

        return sb.ToString();
    }
}