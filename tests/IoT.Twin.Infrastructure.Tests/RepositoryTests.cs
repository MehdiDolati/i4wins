using IoT.Twin.Domain.Entities;
using IoT.Twin.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IoT.Twin.Infrastructure.Tests;

public class RepositoryTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly ReadingRepository _sut;

    public RepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _sut = new ReadingRepository(_db);
    }

    public void Dispose() => _db.Dispose();

    private static Reading CreateReading(string device = "PUMP-01", string metric = "temperature",
        double value = 67.0, long seq = 1, DateTime? ts = null, bool anomaly = false)
        => new()
        {
            DeviceId = device,
            Metric = metric,
            Value = value,
            SequenceNumber = seq,
            Timestamp = ts ?? DateTime.Parse("2025-06-01T08:00:00Z"),
            IsAnomaly = anomaly
        };

    [Fact]
    public async Task AddRangeAsync_StoresReadings()
    {
        var readings = new List<Reading>
        {
            CreateReading(seq: 1),
            CreateReading(seq: 2)
        };

        await _sut.AddRangeAsync(readings);

        Assert.Equal(2, await _db.Readings.CountAsync());
    }

    [Fact]
    public async Task GetAllDeviceIdsAsync_ReturnsDistinctDevices()
    {
        await _sut.AddRangeAsync(new List<Reading>
        {
            CreateReading(device: "PUMP-01", seq: 1),
            CreateReading(device: "PUMP-01", seq: 2),
            CreateReading(device: "PUMP-02", seq: 3)
        });

        var result = await _sut.GetAllDeviceIdsAsync();

        Assert.Equal(2, result.Count);
        Assert.Contains("PUMP-01", result);
        Assert.Contains("PUMP-02", result);
    }

    [Fact]
    public async Task GetLatestAsync_ReturnsMostRecent()
    {
        await _sut.AddRangeAsync(new List<Reading>
        {
            CreateReading(seq: 1, ts: DateTime.Parse("2025-06-01T08:00:00Z")),
            CreateReading(seq: 2, ts: DateTime.Parse("2025-06-01T08:10:00Z"))
        });

        var result = await _sut.GetLatestAsync("PUMP-01", "temperature");

        Assert.NotNull(result);
        Assert.Equal(2, result!.SequenceNumber);
    }

    [Fact]
    public async Task GetReadingsAsync_FiltersByDevice()
    {
        await _sut.AddRangeAsync(new List<Reading>
        {
            CreateReading(device: "PUMP-01", seq: 1),
            CreateReading(device: "PUMP-02", seq: 2)
        });

        var result = await _sut.GetReadingsAsync("PUMP-01", null, null, null);

        Assert.Single(result);
        Assert.Equal("PUMP-01", result[0].DeviceId);
    }

    [Fact]
    public async Task ExistsBySequenceAsync_TrueForExisting()
    {
        await _sut.AddRangeAsync(new List<Reading>
        {
            CreateReading(device: "PUMP-01", metric: "temperature", seq: 100)
        });

        var exists = await _sut.ExistsBySequenceAsync("PUMP-01", "temperature", 100);
        var notExists = await _sut.ExistsBySequenceAsync("PUMP-01", "temperature", 999);

        Assert.True(exists);
        Assert.False(notExists);
    }

    [Fact]
    public async Task GetAnomaliesAsync_OnlyReturnsAnomalies()
    {
        await _sut.AddRangeAsync(new List<Reading>
        {
            CreateReading(seq: 1, anomaly: false),
            CreateReading(seq: 2, anomaly: true),
            CreateReading(seq: 3, anomaly: true)
        });

        var result = await _sut.GetAnomaliesAsync(null, null, null, null);

        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.True(r.IsAnomaly));
    }
}
