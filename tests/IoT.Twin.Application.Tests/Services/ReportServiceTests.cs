using IoT.Twin.Application.Services;
using IoT.Twin.Domain.Entities;
using IoT.Twin.Domain.Interfaces;
using Moq;

namespace IoT.Twin.Application.Tests.Services;

public class ReportServiceTests
{
    private readonly Mock<IReadingRepository> _mockRepo;
    private readonly ReportService _sut;

    public ReportServiceTests()
    {
        _mockRepo = new Mock<IReadingRepository>();
        _sut = new ReportService(_mockRepo.Object);
    }

    private static Reading CreateReading(string device, string metric, double value, DateTime ts, long seq, bool anomaly = false)
        => new() { DeviceId = device, Metric = metric, Value = value, Timestamp = ts, SequenceNumber = seq, IsAnomaly = anomaly };

    [Fact]
    public async Task GetAggregation_ReturnsCorrectStats()
    {
        var readings = new List<Reading>
        {
            CreateReading("PUMP-01", "temperature", 67.0, DateTime.Parse("2025-06-01T08:00:00Z"), 1),
            CreateReading("PUMP-01", "temperature", 68.0, DateTime.Parse("2025-06-01T08:01:00Z"), 2),
            CreateReading("PUMP-01", "temperature", 69.0, DateTime.Parse("2025-06-01T08:02:00Z"), 3)
        };
        _mockRepo.Setup(r => r.GetReadingsAsync(null, null, null, null)).ReturnsAsync(readings);

        var result = await _sut.GetAggregationAsync(null, null, null, null);

        Assert.Single(result);
        Assert.Equal(3, result[0].Count);
        Assert.Equal(67.0, result[0].Min);
        Assert.Equal(69.0, result[0].Max);
        Assert.Equal(68.0, result[0].Average);
    }

    [Fact]
    public async Task GetAggregation_GroupsByDeviceAndMetric()
    {
        var readings = new List<Reading>
        {
            CreateReading("PUMP-01", "temperature", 67.0, DateTime.Parse("2025-06-01T08:00:00Z"), 1),
            CreateReading("PUMP-01", "vibration", 5.0, DateTime.Parse("2025-06-01T08:00:00Z"), 2),
            CreateReading("PUMP-02", "temperature", 70.0, DateTime.Parse("2025-06-01T08:00:00Z"), 3)
        };
        _mockRepo.Setup(r => r.GetReadingsAsync(null, null, null, null)).ReturnsAsync(readings);

        var result = await _sut.GetAggregationAsync(null, null, null, null);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetTimeSeries_ReturnsOrderedPoints()
    {
        var readings = new List<Reading>
        {
            CreateReading("PUMP-01", "temperature", 69.0, DateTime.Parse("2025-06-01T08:02:00Z"), 3),
            CreateReading("PUMP-01", "temperature", 67.0, DateTime.Parse("2025-06-01T08:00:00Z"), 1),
            CreateReading("PUMP-01", "temperature", 68.0, DateTime.Parse("2025-06-01T08:01:00Z"), 2)
        };
        _mockRepo.Setup(r => r.GetReadingsAsync(null, null, null, null)).ReturnsAsync(readings);

        var result = await _sut.GetTimeSeriesAsync(null, null, null, null);

        Assert.Single(result);
        Assert.Equal(3, result[0].Points.Count);
        Assert.True(result[0].Points[0].Timestamp < result[0].Points[1].Timestamp);
    }

    [Fact]
    public async Task GetAnomalies_OnlyReturnsAnomalies()
    {
        var readings = new List<Reading>
        {
            CreateReading("FAN-03", "vibration", -9999.0, DateTime.Parse("2025-06-01T08:05:00Z"), 1, true),
            CreateReading("FAN-03", "vibration", 2.5, DateTime.Parse("2025-06-01T08:06:00Z"), 2, false)
        };
        _mockRepo.Setup(r => r.GetAnomaliesAsync(null, null, null, null))
            .ReturnsAsync(readings.Where(r => r.IsAnomaly).ToList());

        var result = await _sut.GetAnomaliesAsync(null, null, null, null);

        Assert.Single(result);
        Assert.Equal(-9999.0, result[0].Value);
    }
}
