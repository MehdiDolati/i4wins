using System.Text.Json;
using Xunit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using i4Twins.Application.Services;
using i4Twins.Application.Interfaces;
using i4Twins.Application.DTOs;
using i4Twins.Domain.Entities;
using i4Twins.Domain.Exceptions;
using i4Twins.Infrastructure.Data;

namespace i4Twins.Tests.IntegrationTests;

public class FileProcessingTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly string _testDataDir;
    private readonly string _connectionString;
    private SqliteReadingRepository? _repository;

    public FileProcessingTests()
    {
        _testDataDir = Path.Combine(Path.GetTempPath(), "i4twins_tests");
        Directory.CreateDirectory(_testDataDir);

        _testDbPath = Path.Combine(_testDataDir, "test_readings.db");
        _connectionString = $"Data Source={_testDbPath};";

        // Initialize database
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection(_connectionString);
        connection.Open();
        var initializer = new DatabaseInitializer(connection);
        initializer.Initialize();
    }

    private IReadingRepository GetRepository()
    {
        _repository = new SqliteReadingRepository(_connectionString);
        return _repository;
    }

    [Fact]
    public async Task ProcessFileAsync_ShouldSuccessfullyProcessValidFile()
    {
        // Arrange
        var repo = GetRepository();
        var logger = NullLogger<ReadingService>.Instance;
        var service = new ReadingService(repo, logger);

        // Create a test file with valid data
        var testFilePath = Path.Combine(_testDataDir, "valid_readings.jsonl");
        await File.WriteAllLinesAsync(testFilePath, new[]
        {
            "{\"deviceId\":\"PUMP-01\",\"metric\":\"temperature\",\"ts\":\"2025-06-01T08:33:00Z\",\"value\":67.21,\"seq\":1199}",
            "{\"deviceId\":\"FAN-03\",\"metric\":\"vibration\",\"ts\":\"2025-06-01T08:32:40Z\",\"value\":5.237,\"seq\":2877}",
            "{\"deviceId\":\"COMP-01\",\"metric\":\"temperature\",\"ts\":\"2025-06-01T08:31:20Z\",\"value\":67.923,\"seq\":2239}",
            "{\"deviceId\":\"COMP-01\",\"metric\":\"pressure\",\"ts\":\"2025-06-01T08:26:30Z\",\"value\":2.929,\"seq\":2420}"
        });

        // Act
        var report = await service.ProcessFileAsync(testFilePath);

        // Assert
        Assert.Equal(4, report.TotalLines);
        Assert.Equal(4, report.StoredCount);
        Assert.Equal(0, report.DuplicatesSkipped);
        Assert.Equal(0, report.InvalidRecords);

        // Verify data was actually stored
        var from = new DateTime(2025, 6, 1, 8, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2025, 6, 1, 9, 0, 0, DateTimeKind.Utc);
        
        var stored = await repo.GetReadingsInRangeAsync("PUMP-01", "temperature", from, to);
        Assert.Single(stored);
        Assert.Equal(67.21, stored.First().Value);
    }

    [Fact]
    public async Task ProcessFileAsync_ShouldHandleDuplicateRecords()
    {
        // Arrange
        var repo = GetRepository();
        var logger = NullLogger<ReadingService>.Instance;
        var service = new ReadingService(repo, logger);

        var testFilePath = Path.Combine(_testDataDir, "duplicate_readings.jsonl");
        await File.WriteAllLinesAsync(testFilePath, new[]
        {
            "{\"deviceId\":\"PUMP-01\",\"metric\":\"temperature\",\"ts\":\"2025-06-01T08:33:00Z\",\"value\":67.21,\"seq\":1199}",
            "{\"deviceId\":\"PUMP-01\",\"metric\":\"temperature\",\"ts\":\"2025-06-01T08:33:00Z\",\"value\":68.00,\"seq\":1199}", // Duplicate identity, different value
            "{\"deviceId\":\"PUMP-01\",\"metric\":\"temperature\",\"ts\":\"2025-06-01T08:34:00Z\",\"value\":67.50,\"seq\":1200}" // Different identity
        });

        // Act
        var report = await service.ProcessFileAsync(testFilePath);

        // Assert
        Assert.Equal(3, report.TotalLines);
        Assert.Equal(2, report.StoredCount); // First and third are stored
        Assert.Equal(1, report.DuplicatesSkipped); // Second is duplicate
        Assert.Equal(0, report.InvalidRecords);

        // Verify duplicate was not stored
        var from = new DateTime(2025, 6, 1, 8, 30, 0, DateTimeKind.Utc);
        var to = new DateTime(2025, 6, 1, 9, 0, 0, DateTimeKind.Utc);
        var stored = await repo.GetReadingsInRangeAsync("PUMP-01", "temperature", from, to);
        
        Assert.Equal(2, stored.Count());
        Assert.Contains(stored, r => r.Sequence == 1199 && r.Value == 67.21);
        Assert.DoesNotContain(stored, r => r.Sequence == 1199 && r.Value == 68.00);
    }

    [Fact]
    public async Task ProcessFileAsync_ShouldHandleInvalidRecords()
    {
        // Arrange
        var repo = GetRepository();
        var logger = NullLogger<ReadingService>.Instance;
        var service = new ReadingService(repo, logger);

        var testFilePath = Path.Combine(_testDataDir, "invalid_readings.jsonl");
        await File.WriteAllLinesAsync(testFilePath, new[]
        {
            "{\"deviceId\":\"PUMP-01\",\"metric\":\"temperature\",\"ts\":\"2025-06-01T08:33:00Z\",\"value\":67.21,\"seq\":1199}", // Valid
            "{\"deviceId\":\"PUMP-01\",\"metric\":\"temperature\",\"ts\":\"2025-06-01T08:33:00Z\",\"value\":null,\"seq\":1199}", // Invalid: null value
            "{\"deviceId\":\"\",\"metric\":\"temperature\",\"ts\":\"2025-06-01T08:33:00Z\",\"value\":67.21,\"seq\":1199}", // Invalid: empty deviceId
            "{\"deviceId\":\"PUMP-01\",\"metric\":\"temperature\",\"ts\":\"2025-06-31T08:33:00Z\",\"value\":67.21,\"seq\":1199}", // Invalid: invalid date
            "{\"deviceId\":\"PUMP-01\",\"metric\":\"temperature\",\"ts\":\"2025-06-01T08:33:00Z\",\"value\":67.21,\"seq\":-1}", // Invalid: negative sequence
            "{\"deviceId\":\"PUMP-01\",\"metric\":\"temperature\",\"ts\":\"2025-06-01T08:33:00Z\",\"value\":\"not-a-number\",\"seq\":1199}" // Invalid: string value
        });

        // Act
        var report = await service.ProcessFileAsync(testFilePath);

        // Assert
        Assert.Equal(6, report.TotalLines);
        Assert.Equal(1, report.StoredCount); // Only first is valid
        Assert.Equal(0, report.DuplicatesSkipped);
        Assert.Equal(5, report.InvalidRecords);
    }

    [Fact]
    public async Task ProcessFileAsync_ShouldHandleOutOfOrderTimestamps()
    {
        // Arrange
        var repo = GetRepository();
        var logger = NullLogger<ReadingService>.Instance;
        var service = new ReadingService(repo, logger);

        var testFilePath = Path.Combine(_testDataDir, "outoforder_readings.jsonl");
        await File.WriteAllLinesAsync(testFilePath, new[]
        {
            "{\"deviceId\":\"PUMP-01\",\"metric\":\"temperature\",\"ts\":\"2025-06-01T08:30:00Z\",\"value\":70.0,\"seq\":1003}",
            "{\"deviceId\":\"PUMP-01\",\"metric\":\"temperature\",\"ts\":\"2025-06-01T08:15:00Z\",\"value\":68.0,\"seq\":1002}",
            "{\"deviceId\":\"PUMP-01\",\"metric\":\"temperature\",\"ts\":\"2025-06-01T08:45:00Z\",\"value\":72.0,\"seq\":1004}",
            "{\"deviceId\":\"PUMP-01\",\"metric\":\"temperature\",\"ts\":\"2025-06-01T08:00:00Z\",\"value\":65.0,\"seq\":1001}"
        });

        // Act
        var report = await service.ProcessFileAsync(testFilePath);

        // Assert
        Assert.Equal(4, report.TotalLines);
        Assert.Equal(4, report.StoredCount);
        Assert.Equal(0, report.DuplicatesSkipped);
        Assert.Equal(0, report.InvalidRecords);

        // Verify data is stored but returned sorted by timestamp
        var from = new DateTime(2025, 6, 1, 7, 30, 0, DateTimeKind.Utc);
        var to = new DateTime(2025, 6, 1, 9, 0, 0, DateTimeKind.Utc);
        var stored = await repo.GetReadingsInRangeAsync("PUMP-01", "temperature", from, to);
        
        var list = stored.ToList();
        Assert.Equal(4, list.Count);
        
        // Should be sorted by timestamp (oldest first)
        Assert.Equal(new DateTime(2025, 6, 1, 8, 0, 0, DateTimeKind.Utc), list[0].Timestamp);
        Assert.Equal(new DateTime(2025, 6, 1, 8, 15, 0, DateTimeKind.Utc), list[1].Timestamp);
        Assert.Equal(new DateTime(2025, 6, 1, 8, 30, 0, DateTimeKind.Utc), list[2].Timestamp);
        Assert.Equal(new DateTime(2025, 6, 1, 8, 45, 0, DateTimeKind.Utc), list[3].Timestamp);
    }

    [Fact]
    public async Task ProcessFileAsync_ShouldHandleLargeFile()
    {
        // Arrange
        var repo = GetRepository();
        var logger = NullLogger<ReadingService>.Instance;
        var service = new ReadingService(repo, logger);

        var testFilePath = Path.Combine(_testDataDir, "large_readings.jsonl");
        var lines = new List<string>();
        var rng = new Random();
        
        // Generate 1000 valid readings
        for (int i = 0; i < 1000; i++)
        {
            var device = new[] { "PUMP-01", "PUMP-02", "COMP-01", "FAN-03" }[rng.Next(4)];
            var metric = new[] { "temperature", "pressure", "vibration" }[rng.Next(3)];
            var timestamp = new DateTime(2025, 6, 1, 8, rng.Next(60), rng.Next(60), DateTimeKind.Utc);
            var value = Math.Round(rng.NextDouble() * 100, 3);
            var seq = i + 1;

            var reading = new
            {
                deviceId = device,
                metric = metric,
                ts = timestamp.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                value = value,
                seq = seq
            };

            lines.Add(JsonSerializer.Serialize(reading));
        }

        await File.WriteAllLinesAsync(testFilePath, lines);

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var report = await service.ProcessFileAsync(testFilePath);
        stopwatch.Stop();

        // Assert
        Assert.Equal(1000, report.TotalLines);
        Assert.Equal(1000, report.StoredCount);
        Assert.Equal(0, report.DuplicatesSkipped);
        Assert.Equal(0, report.InvalidRecords);
        
        // Performance check: should process 1000 records in under 5 seconds
        Assert.True(stopwatch.ElapsedMilliseconds < 5000, 
            $"Processing took {stopwatch.ElapsedMilliseconds}ms, which is too slow");

        // Verify all data was stored
        var from = new DateTime(2025, 6, 1, 7, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2025, 6, 1, 10, 0, 0, DateTimeKind.Utc);
        
        var allStored = await repo.GetReadingsInRangeAsync("PUMP-01", "temperature", from, to);
        // Should have some readings for PUMP-01 (not all, since random)
        Assert.True(allStored.Any(), "No readings found for PUMP-01");
    }

    [Fact]
    public async Task ProcessFileAsync_ShouldReturnCorrectReport()
    {
        // Arrange
        var repo = GetRepository();
        var logger = NullLogger<ReadingService>.Instance;
        var service = new ReadingService(repo, logger);

        var testFilePath = Path.Combine(_testDataDir, "mixed_readings.jsonl");
        await File.WriteAllLinesAsync(testFilePath, new[]
        {
            // Valid
            "{\"deviceId\":\"PUMP-01\",\"metric\":\"temperature\",\"ts\":\"2025-06-01T08:33:00Z\",\"value\":67.21,\"seq\":1199}",
            // Valid
            "{\"deviceId\":\"FAN-03\",\"metric\":\"vibration\",\"ts\":\"2025-06-01T08:32:40Z\",\"value\":5.237,\"seq\":2877}",
            // Invalid: duplicate of first
            "{\"deviceId\":\"PUMP-01\",\"metric\":\"temperature\",\"ts\":\"2025-06-01T08:33:00Z\",\"value\":67.21,\"seq\":1199}",
            // Valid
            "{\"deviceId\":\"COMP-01\",\"metric\":\"pressure\",\"ts\":\"2025-06-01T08:26:30Z\",\"value\":2.929,\"seq\":2420}",
            // Invalid: empty deviceId
            "{\"deviceId\":\"\",\"metric\":\"temperature\",\"ts\":\"2025-06-01T08:33:00Z\",\"value\":67.21,\"seq\":1199}",
            // Invalid: negative sequence
            "{\"deviceId\":\"PUMP-01\",\"metric\":\"temperature\",\"ts\":\"2025-06-01T08:33:00Z\",\"value\":67.21,\"seq\":-5}"
        });

        // Act
        var report = await service.ProcessFileAsync(testFilePath);

        // Assert
        Assert.Equal(6, report.TotalLines);
        Assert.Equal(3, report.StoredCount); // Lines 1, 2, 4
        Assert.Equal(1, report.DuplicatesSkipped); // Line 3
        Assert.Equal(2, report.InvalidRecords); // Lines 5, 6

        // Verify report string representation
        var reportString = report.ToString();
        Assert.Contains("Total: 6", reportString);
        Assert.Contains("Stored: 3", reportString);
        Assert.Contains("Duplicates: 1", reportString);
        Assert.Contains("Invalid: 2", reportString);
    }

    [Fact]
    public async Task ProcessFileAsync_WithEmptyFile_ShouldReturnZeroCounts()
    {
        // Arrange
        var repo = GetRepository();
        var logger = NullLogger<ReadingService>.Instance;
        var service = new ReadingService(repo, logger);

        var testFilePath = Path.Combine(_testDataDir, "empty_readings.jsonl");
        await File.WriteAllTextAsync(testFilePath, "");

        // Act
        var report = await service.ProcessFileAsync(testFilePath);

        // Assert
        Assert.Equal(0, report.TotalLines);
        Assert.Equal(0, report.StoredCount);
        Assert.Equal(0, report.DuplicatesSkipped);
        Assert.Equal(0, report.InvalidRecords);
    }

    [Fact]
    public async Task ProcessFileAsync_WithOnlyWhitespaceLines_ShouldIgnoreThem()
    {
        // Arrange
        var repo = GetRepository();
        var logger = NullLogger<ReadingService>.Instance;
        var service = new ReadingService(repo, logger);

        var testFilePath = Path.Combine(_testDataDir, "whitespace_readings.jsonl");
        await File.WriteAllLinesAsync(testFilePath, new[]
        {
            "",
            "   ",
            "\t",
            "{\"deviceId\":\"PUMP-01\",\"metric\":\"temperature\",\"ts\":\"2025-06-01T08:33:00Z\",\"value\":67.21,\"seq\":1199}",
            ""
        });

        // Act
        var report = await service.ProcessFileAsync(testFilePath);

        // Assert
        Assert.Equal(5, report.TotalLines);
        Assert.Equal(1, report.StoredCount);
        Assert.Equal(0, report.DuplicatesSkipped);
        Assert.Equal(0, report.InvalidRecords); // Whitespace lines don't count as invalid
    }

    [Fact]
    public async Task ProcessFileAsync_WithMixedValidAndInvalid_ShouldProcessCorrectly()
    {
        // Arrange
        var repo = GetRepository();
        var logger = NullLogger<ReadingService>.Instance;
        var service = new ReadingService(repo, logger);

        var testFilePath = Path.Combine(_testDataDir, "mixed_readings2.jsonl");
        await File.WriteAllLinesAsync(testFilePath, new[]
        {
            "{\"deviceId\":\"PUMP-01\",\"metric\":\"temperature\",\"ts\":\"2025-06-01T08:33:00Z\",\"value\":67.21,\"seq\":1199}",
            "{\"deviceId\":\"PUMP-01\",\"metric\":\"temperature\",\"ts\":\"2025-06-01T08:33:00Z\",\"value\":68.00,\"seq\":1199}", // Duplicate
            "{\"deviceId\":\"PUMP-02\",\"metric\":\"pressure\",\"ts\":\"2025-06-01T08:22:10Z\",\"value\":\"invalid\",\"seq\":1974}", // Invalid value type
            "{\"deviceId\":\"PUMP-02\",\"metric\":\"pressure\",\"ts\":\"2025-06-01T08:22:10Z\",\"value\":10.885,\"seq\":1974}", // Valid
        });

        // Act
        var report = await service.ProcessFileAsync(testFilePath);

        // Assert
        Assert.Equal(4, report.TotalLines);
        Assert.Equal(2, report.StoredCount); // Lines 1 and 4
        Assert.Equal(1, report.DuplicatesSkipped); // Line 2
        Assert.Equal(1, report.InvalidRecords); // Line 3
    }

    public void Dispose()
    {
        _repository?.Dispose();
        
        // Clean up test files
        if (Directory.Exists(_testDataDir))
        {
            try
            {
                Directory.Delete(_testDataDir, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}