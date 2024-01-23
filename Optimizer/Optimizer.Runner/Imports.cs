using Optimizer.Logic;

namespace Optimizer.Runner;
using System.IO;
using System.Linq;
using OfficeOpenXml;


public static class Imports
{
    public static (Input, Solution) ImportData(string defenseFilePath, string chairpersonsFilePath)
    {
        var input = new Input();
        var solution = new Solution();

        // Import data from obrony.xlsx file
        using (var excelPackage = new ExcelPackage(new FileInfo(defenseFilePath)))
        {
            var worksheet = excelPackage.Workbook.Worksheets.First();
            var rowCount = worksheet.Dimension.Rows;

            for (var row = 2; row <= rowCount; row++)
            {
                // var lastName = worksheet.Cells[row, 1].Value?.ToString();
                // var firstName = worksheet.Cells[row, 2].Value?.ToString();
                var supervisor = worksheet.Cells[row, 7].Value?.ToString();
                var reviewer = worksheet.Cells[row, 8].Value?.ToString();

                // Convert names to IDs using Solution class
                byte supervisorId = solution.PersonId(supervisor);
                byte reviewerId = solution.PersonId(reviewer);

                // Add to input
                input.DefensesToAssign = input.DefensesToAssign.Append(new InputCombination
                {
                    ReviewerId = reviewerId,
                    PromoterId = supervisorId,
                    TotalCount = 1
                }).ToArray();
            }
        }

        // Import data from chairpersons file
        using (var excelPackage = new ExcelPackage(new FileInfo(chairpersonsFilePath)))
        {
            var worksheet = excelPackage.Workbook.Worksheets.First();
            var rowCount = worksheet.Dimension.Rows;

            for (var row = 2; row <= rowCount; row++)
            {
                var chairpersonName = worksheet.Cells[row, 1].Value?.ToString();
                // Convert chairperson name to ID using Solution class
                byte chairpersonId = solution.PersonId(chairpersonName);

                // Add to available chairpersons
                input.AvailableChairPersonIds = input.AvailableChairPersonIds.Append(chairpersonId).ToArray();
            }
        }

        // Add days and classrooms to input (you may need to modify this based on your actual structure)
        input.Days = new[]
        {
            new InputDay
            {
                Id = 1,
                Classrooms = new[]
                {
                    new InputClassroom(0, slots: 17, 8, 9),
                    new InputClassroom(1, slots: 16, 8, 8),
                },
            },
            new InputDay
            {
                Id = 2,
                Classrooms = new[]
                {
                    new InputClassroom(0, slots: 16, 8, 8),
                    new InputClassroom(1, slots: 16, 8, 8),
                },
            },
        };

        return (input, solution);
    }
}
