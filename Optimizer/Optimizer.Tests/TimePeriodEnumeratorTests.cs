using Optimizer.Runner;

namespace Optimizer.Tests;

public class TimePeriodExtensionsTests
{
    [Test]
    public void SlotIndices_ReturnsCorrectIndices()
    {
        // Arrange
        var settingValue = new List<TimePeriod>
        {
            new(new TimeOnly(7, 0, 0), new TimeOnly(10, 0, 0)),
            new(new TimeOnly(11, 0, 0), new TimeOnly(12, 30, 0))
        };

        // Act
        var indices = settingValue.SlotIndices();

        // Assert
        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 6, 7, 8 }, indices);
    }


    [Test]
    public void SlotIndices_ReturnsCorrectIndices2()
    {
        // Arrange
        var settingValue = new List<TimePeriod>
        {
            new(new TimeOnly(8, 0, 1), new TimeOnly(9, 0, 1)),
        };

        // Act
        var indices = settingValue.SlotIndices();

        // Assert
        CollectionAssert.AreEqual(new[] { 0, 1, 2 }, indices);
    }
}