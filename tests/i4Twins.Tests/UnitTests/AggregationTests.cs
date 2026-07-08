using Moq;
using Xunit;
using Microsoft.Extensions.Logging.Abstractions;
using i4Twins.Application.Services;
using i4Twins.Application.Interfaces;
using i4Twins.Domain.Entities;

namespace i4Twins.Tests.UnitTests;

public class AggregationTests
{
    [Fact]
    public async Task GetAggregatedDataAsync_ShouldCalculateCorrectAggregates()
    {
        // Arrange
        var readings = new List<Reading>
        {
            Reading.Create("PUMP-01", "temperature", new DateTime(2025, 6, 1, 8, 0, 0, DateTimeKind.Utc), 10.0, 1),
            Reading.Create("PUMP-01", "temperature", new DateTime(2025, 6, 1, 8, 15, 0, DateTimeKind.Utc), 20.0, 2),
            Reading.Create("PUMP-01", "temperature", new DateTime(2025, 6, 1, 8, 30, 0, DateTimeKind.Utc), 30.0, 3),
            Reading.Create("PUMP-01", "temperature", new DateTime(2025, 6, 1, 8, 45, 0, DateTimeKind.Utc), 40.0, 4)
        };

        var mockRepo = new Mock<IReadingRepository>();
        mockRepo.Setup(r => r.GetReadingsInRangeAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(readings);

        var logger = NullLogger<ReadingService>.Instance;
        var service = new ReadingService(mockRepo.Object, logger);

        var from = new DateTime(2025, 6, 1, 8, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2025, 6, 1, 9, 0, 0, DateTimeKind.Utc);

        // Act
        var result = await service.GetAggregatedDataAsync(
            "PUMP-01", "temperature", from, to, 3600);

        // Assert
        Assert.Single(result.Buckets);
        var bucket = result.Buckets.First();
        Assert.Equal(4, bucket.Count);
        Assert.Equal(25.0, bucket.Average);
        Assert.Equal(10.0, bucket.Minimum);
        Assert.Equal(40.0, bucket.Maximum);
    }

    [Fact]
    public async Task GetAggregatedDataAsync_ShouldReturnEmptyBuckets_WhenNoData()
    {
        // Arrange
        var mockRepo = new Mock<IReadingRepository>();
        mockRepo.Setup(r => r.GetReadingsInRangeAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Reading>());

        var logger = NullLogger<ReadingService>.Instance;
        var service = new ReadingService(mockRepo.Object, logger);

        var from = new DateTime(2025, 6, 1, 8, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2025, 6, 1, 9, 0, 0, DateTimeKind.Utc);

        // Act
        var result = await service.GetAggregatedDataAsync(
            "PUMP-01", "temperature", from, to, 3600);

        // Assert
        Assert.Empty(result.Buckets);
    }

    [Fact]
    public async Task GetAggregatedDataAsync_ShouldHandleMultipleBuckets()
    {
        // Arrange
        var readings = new List<Reading>
        {
            Reading.Create("PUMP-01", "temperature", new DateTime(2025, 6, 1, 8, 0, 0, DateTimeKind.Utc), 10.0, 1),
            Reading.Create("PUMP-01", "temperature", new DateTime(2025, 6, 1, 8, 30, 0, DateTimeKind.Utc), 20.0, 2),
            Reading.Create("PUMP-01", "temperature", new DateTime(2025, 6, 1, 9, 0, 0, DateTimeKind.Utc), 30.0, 3)
        };

        var mockRepo = new Mock<IReadingRepository>();
        mockRepo.Setup(r => r.GetReadingsInRangeAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(readings);

        var logger = NullLogger<ReadingService>.Instance;
        var service = new ReadingService(mockRepo.Object, logger);

        var from = new DateTime(2025, 6, 1, 8, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2025, 6, 1, 10, 0, 0, DateTimeKind.Utc);

        // Act
        var result = await service.GetAggregatedDataAsync(
            "PUMP-01", "temperature", from, to, 3600);

        // Assert
        Assert.Equal(2, result.Buckets.Count); // 8-9 and 9-10
        Assert.Equal(2, result.Buckets[0].Count);
        Assert.Equal(15.0, result.Buckets[0].Average);
        Assert.Equal(1, result.Buckets[1].Count);
        Assert.Equal(30.0, result.Buckets[1].Average);
    }
}