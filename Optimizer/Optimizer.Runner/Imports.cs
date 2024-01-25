using Optimizer.Logic;

namespace Optimizer.Runner;

using System.IO;
using System.Linq;
using OfficeOpenXml;

public static class Imports
{
    public static Solution ImportData(FileInfo defenseFilePath, FileInfo chairpersonsFilePath, FileInfo roomsFilePath, FileInfo absencesFilePath)
    {
        var solution = new Solution();

        // Chairpersons
        using (var excelPackage = new ExcelPackage(chairpersonsFilePath))
        {
            var worksheet = excelPackage.Workbook.Worksheets.First();
            var rowCount = worksheet.Dimension.Rows;
            for (var row = 2; row <= rowCount; row++)
            {
                var chairpersonName = worksheet.Cells[row, 1].Value?.ToString();
                solution.AddChairPerson(chairpersonName!);
            }
        }

        // Defenses
        using (var excelPackage = new ExcelPackage(defenseFilePath))
        {
            var worksheet = excelPackage.Workbook.Worksheets.First();
            var rowCount = worksheet.Dimension.Rows;

            for (var row = 2; row <= rowCount; row++)
            {
                var lastName = worksheet.Cells[row, 1].Value?.ToString();
                var firstName = worksheet.Cells[row, 2].Value?.ToString();
                var title = worksheet.Cells[row, 3].Value?.ToString();
                var supervisor = worksheet.Cells[row, 7].Value?.ToString();
                var reviewer = worksheet.Cells[row, 8].Value?.ToString();

                if (string.Equals(worksheet.Cells[row, 4].Value.ToString(), "II stopień", StringComparison.InvariantCultureIgnoreCase))
                    continue;

                solution.AddDefenseInfo($"{firstName} {lastName}", title!, supervisor!, reviewer!);
            }
        }

        // Rooms configuration
        using (var excelPackage = new ExcelPackage(roomsFilePath))
        {
            var worksheet = excelPackage.Workbook.Worksheets.First();
            var rowCount = worksheet.Dimension.Rows;

            for (var row = 2; row <= rowCount; row++)
            {
                var day = worksheet.Cells[row, 1].Value.ToString();
                var room = worksheet.Cells[row, 2].Value.ToString();
                var defenses = worksheet.Cells[row, 3].Value.ToString()!.Split(',').Select(byte.Parse);

                solution.AddRoomInfo((byte)(byte.Parse(day!)-1), (byte)(byte.Parse(room!)-1), defenses.ToArray());
            }
        }

        // People absences
        using (var excelPackage = new ExcelPackage(absencesFilePath))
        {
            var worksheet = excelPackage.Workbook.Worksheets.First();
            var rowCount = worksheet.Dimension.Rows;

            for (var row = 2; row <= rowCount; row++)
            {
                var person = worksheet.Cells[row, 1].Value.ToString();
                var date = worksheet.Cells[row, 2].Value.ToString();
                var span = worksheet.Cells[row, 3].Value.ToString();

                solution.AddAbsence(person!, DateOnly.ParseExact(date!, "dd-MM-yyyy"), TimePeriod.Parse(span!));
            }
        }

        return solution;
    }
}