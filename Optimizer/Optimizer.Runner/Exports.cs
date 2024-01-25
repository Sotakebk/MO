using System.Text;
using OfficeOpenXml;

namespace Optimizer.Runner;

public static class Exports
{
    private static Solution.FullDefenseInfo GetDebug(Solution solution, Logic.Work.AssignmentOptimization.SolutionSlot assignment)
    {
        return new Solution.FullDefenseInfo("", "", assignment.A?.ToString(), assignment.B?.ToString(), assignment.ChairPersonId.ToString());
    }

    private static Solution.FullDefenseInfo GetPretty(Solution solution, Logic.Work.AssignmentOptimization.SolutionSlot assignment)
    {
        if (!assignment.A.HasValue || !assignment.B.HasValue)
            return new Solution.FullDefenseInfo();

        return solution.GetFullDefenseInfo(assignment.A.Value, assignment.B.Value, assignment.ChairPersonId);
    }

    public static async Task WriteToXlsx(Solution solution, string fileName, Logic.Work.AssignmentOptimization.OptimizerOutput result, bool overwrite = true, bool debug = false)
    {
        var colorPalette = Colors.GenerateColorPalette(30);

        if (overwrite && File.Exists(fileName))
            File.Delete(fileName);
        using var p = new ExcelPackage(fileName);
        var ws = p.Workbook.Worksheets.Add("Harmonogram");
        var currentRow = 1;

        ws.DefaultColWidth = 18;
        ws.DefaultRowHeight = 18;

        for (var dayIndex = 0; dayIndex < result.Days.Length; dayIndex++)
        {
            var day = result.Days[dayIndex];
            ws.Cells[currentRow, 1, currentRow, 6 * day.Classrooms.Length].Merge = true;
            ws.Cells[currentRow, 1].Value = $"Dzień {dayIndex + 1}";
            ws.Cells[currentRow, 1].Style.Fill.SetBackground(OfficeOpenXml.Drawing.eThemeSchemeColor.Accent2);
            ws.Cells[currentRow, 1].Style.Fill.BackgroundColor.Tint = 0.6M;
            currentRow++;


            var maxRows = 0;

            for (var roomIndex = 0; roomIndex < day.Classrooms.Length; roomIndex++)
            {
                ws.Columns[roomIndex * 6 + 1].Width = 8;
                ws.Columns[roomIndex * 6 + 6].Width = 32;
                ws.Columns[roomIndex * 6 + 6].Hidden = true;

                var subCurrentRow = 0;
                var classroom = day.Classrooms[roomIndex];
                ws.Cells[currentRow + subCurrentRow, roomIndex * 6 + 1, currentRow, roomIndex * 6 + 6].Merge = true;
                ws.Cells[currentRow + subCurrentRow, roomIndex * 6 + 1].Text($"Sala {roomIndex + 1}").Bold();

                ws.Cells[currentRow + subCurrentRow, roomIndex * 6 + 1].Style.Fill.SetBackground(OfficeOpenXml.Drawing.eThemeSchemeColor.Accent2);
                ws.Cells[currentRow + subCurrentRow, roomIndex * 6 + 1].Style.Fill.BackgroundColor.Tint = 0.4M;
                subCurrentRow++;

                ws.Cells[currentRow + subCurrentRow, roomIndex * 6 + 2].Text("Przewodniczący").Bold();
                ws.Cells[currentRow + subCurrentRow, roomIndex * 6 + 3].Text("Promotor").Bold();
                ws.Cells[currentRow + subCurrentRow, roomIndex * 6 + 4].Text("Recenzent").Bold();
                ws.Cells[currentRow + subCurrentRow, roomIndex * 6 + 5].Text("Osoba").Bold();
                ws.Cells[currentRow + subCurrentRow, roomIndex * 6 + 6].Text("Praca").Bold();
                subCurrentRow++;


                for (var slotIndex = 0; slotIndex < classroom.Slots.Length; slotIndex++)
                {
                    var assignment = classroom.Slots[slotIndex];
                    var defense = debug ? GetDebug(solution, assignment) : GetPretty(solution, assignment);

                    ws.Cells[currentRow + subCurrentRow, roomIndex * 6 + 1].Value = new TimeOnly(8, 0, 0).AddMinutes(slotIndex * 30).ToString("HH:mm");
                    ws.Cells[currentRow + subCurrentRow, roomIndex * 6 + 2].Value = defense.Chairperson;
                    ws.Cells[currentRow + subCurrentRow, roomIndex * 6 + 3].Value = defense.Supervisor;
                    ws.Cells[currentRow + subCurrentRow, roomIndex * 6 + 4].Value = defense.Reviewer;
                    ws.Cells[currentRow + subCurrentRow, roomIndex * 6 + 5].Value = defense.Student;
                    ws.Cells[currentRow + subCurrentRow, roomIndex * 6 + 6].Value = defense.Title;
                    ws.Cells[currentRow + subCurrentRow, roomIndex * 6 + 6].Style.WrapText = true;

                    if (debug)
                    {
                        ws.Cells[currentRow + subCurrentRow, roomIndex * 6 + 2].Style.Fill.SetBackground(colorPalette[assignment.ChairPersonId]);
                        
                        if (assignment.A.HasValue)
                            ws.Cells[currentRow + subCurrentRow, roomIndex * 6 + 3].Style.Fill.SetBackground(colorPalette[assignment.A.Value]);
                        if (assignment.B.HasValue)
                            ws.Cells[currentRow + subCurrentRow, roomIndex * 6 + 4].Style.Fill.SetBackground(colorPalette[assignment.B.Value]);
                    }

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

public static class ExcelExtensions
{
    public static ExcelRange Text(this ExcelRange range, string text)
    {
        range.Value = text;
        return range;
    }

    public static ExcelRange Bold(this ExcelRange range)
    {
        range.Style.Font.Bold = true;
        return range;
    }
}