using Moq;
using Xunit;
using Microsoft.Extensions.Logging.Abstractions;
using i4Twins.Application.Services;
using i4Twins.Application.Interfaces;
using i4Twins.Domain.Entities;
using i4Twins.Domain.ValueObjects;

namespace i4Twins.Tests.UnitTests;

public class DeduplicationTests
{
    [Fact]
    public async Task ProcessFileAsync_ShouldSkipDuplicates()
    {
        // Arrange
        var mockRepo = new Mock<IReadingRepository>();
        mockRepo.Setup(r => r.ExistsAsync(It.IsAny<ReadingIdentity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

        var logger = NullLogger<ReadingService>.Instance;
        var service = new ReadingService(mockRepo.Object, logger);

        var tempFile = Path.GetTempFileName();
        await File.WriteAllLinesAsync(tempFile, new[]
        {
            "{\"deviceId\":\"PUMP-01\",\"metric\":\"temperature\",\"ts\":\"2025-06-01T08:33:00Z\",\"value\":67.21,\"seq\":1199}",
            "{\"deviceId\":\"PUMP-01\",\"metric\":\"temperature\",\"ts\":\"2025-06-01T08:33:00Z\",\"value\":67.21,\"seq\":1199}"
        });

        // Act
        var report = await service.ProcessFileAsync(tempFile);

        // Assert
        Assert.Equal(2, report.TotalLines);
        Assert.Equal(2, report.DuplicatesSkipped);
        Assert.Equal(0, report.StoredCount);
    }

    [Fact]
    public async Task ProcessFileAsync_ShouldStoreUniqueReadings()
    {
        // Arrange
        var mockRepo = new Mock<IReadingRepository>();
        mockRepo.Setup(r => r.ExistsAsync(It.IsAny<ReadingIdentity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

        var logger = NullLogger<ReadingService>.Instance;
        var service = new ReadingService(mockRepo.Object, logger);

        var tempFile = Path.GetTempFileName();
        await File.WriteAllLinesAsync(tempFile, new[]
        {
            "{\"deviceId\":\"PUMP-01\",\"metric\":\"temperature\",\"ts\":\"2025-06-01T08:33:00Z\",\"value\":67.21,\"seq\":1199}",
            "{\"deviceId\":\"PUMP-02\",\"metric\":\"pressure\",\"ts\":\"2025-06-01T08:22:10Z\",\"value\":10.885,\"seq\":1974}"
        });

        // Act
        var report = await service.ProcessFileAsync(tempFile);

        // Assert
        Assert.Equal(2, report.TotalLines);
        Assert.Equal(0, report.DuplicatesSkipped);
        Assert.Equal(2, report.StoredCount);
        mockRepo.Verify(r => r.AddAsync(It.IsAny<Reading>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ProcessFileAsync_ShouldHandleInvalidRecords()
    {
        // Arrange
        var mockRepo = new Mock<IReadingRepository>();
        var logger = NullLogger<ReadingService>.Instance;
        var service = new ReadingService(mockRepo.Object, logger);

        var tempFile = Path.GetTempFileName();
        await File.WriteAllLinesAsync(tempFile, new[]
        {
            "{\"deviceId\":\"PUMP-01\",\"metric\":\"temperature\",\"ts\":\"2025-06-01T08:33:00Z\",\"value\":null,\"seq\":1199}", // Invalid: null value
            "{\"deviceId\":\"PUMP-01\",\"metric\":\"temperature\",\"ts\":\"2025-06-01T08:33:00Z\",\"value\":\"invalid\",\"seq\":1199}", // Invalid: wrong type
            "{\"deviceId\":\"\",\"metric\":\"temperature\",\"ts\":\"2025-06-01T08:33:00Z\",\"value\":67.21,\"seq\":1199}" // Invalid: empty deviceId
        });

        // Act
        var report = await service.ProcessFileAsync(tempFile);

        // Assert
        Assert.Equal(3, report.TotalLines);
        Assert.Equal(3, report.InvalidRecords);
        Assert.Equal(0, report.StoredCount);
    }
}