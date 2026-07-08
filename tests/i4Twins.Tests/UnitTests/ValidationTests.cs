using Xunit;
using i4Twins.Domain.Entities;
using i4Twins.Domain.Exceptions;

namespace i4Twins.Tests.UnitTests;

public class ValidationTests
{
    [Theory]
    [InlineData("", "temperature", "2025-06-01T08:33:00Z", 67.21, 1199)]
    [InlineData("PUMP-01", "", "2025-06-01T08:33:00Z", 67.21, 1199)]
    [InlineData("PUMP-01", "temperature", "2025-06-01T08:33:00Z", double.NaN, 1199)]
    [InlineData("PUMP-01", "temperature", "2025-06-01T08:33:00Z", double.PositiveInfinity, 1199)]
    [InlineData("PUMP-01", "temperature", "2025-06-01T08:33:00Z", 67.21, -1)]
    [InlineData("PUMP-01", "temperature", "2025-06-01T08:33:00Z", 67.21, 0)]
    public void Create_WithInvalidData_ShouldThrowInvalidReadingException(
        string deviceId, string metric, string timestamp, double value, int sequence)
    {
        // Arrange
        var ts = DateTime.Parse(timestamp, null, System.Globalization.DateTimeStyles.RoundtripKind);

        // Act & Assert
        Assert.Throws<InvalidReadingException>(() =>
            Reading.Create(deviceId, metric, ts, value, sequence));
    }

    [Theory]
    [InlineData("PUMP-01", "temperature", "2025-06-01T08:33:00Z", 200.0, 1199)] // Too hot
    [InlineData("PUMP-01", "pressure", "2025-06-01T08:33:00Z", -5.0, 1199)] // Negative pressure
    [InlineData("PUMP-01", "pressure", "2025-06-01T08:33:00Z", 150.0, 1199)] // Too high pressure
    [InlineData("PUMP-01", "vibration", "2025-06-01T08:33:00Z", 50.0, 1199)] // Too high vibration
    public void Create_WithOutOfRangeValues_ShouldThrowInvalidReadingException(
        string deviceId, string metric, string timestamp, double value, int sequence)
    {
        // Arrange
        var ts = DateTime.Parse(timestamp, null, System.Globalization.DateTimeStyles.RoundtripKind);

        // Act & Assert
        Assert.Throws<InvalidReadingException>(() =>
            Reading.Create(deviceId, metric, ts, value, sequence));
    }
}