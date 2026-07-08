using IoT.Twin.Infrastructure.Services;

namespace IoT.Twin.Application.Tests.Services;

public class DataCleanerTests
{
    [Fact]
    public void Clean_NullDeviceId_IsSkipped()
    {
        var records = new List<JsonlRecord>
        {
            new() { DeviceId = null, Metric = "temperature", Ts = "2025-06-01T08:00:00Z", Value = 25.0, Seq = 1 }
        };

        var result = DataCleaner.Clean(records);

        Assert.Empty(result);
    }

    [Fact]
    public void Clean_EmptyDeviceId_IsSkipped()
    {
        var records = new List<JsonlRecord>
        {
            new() { DeviceId = "", Metric = "temperature", Ts = "2025-06-01T08:00:00Z", Value = 25.0, Seq = 1 }
        };

        var result = DataCleaner.Clean(records);

        Assert.Empty(result);
    }

    [Fact]
    public void Clean_NullMetric_IsSkipped()
    {
        var records = new List<JsonlRecord>
        {
            new() { DeviceId = "PUMP-01", Metric = null, Ts = "2025-06-01T08:00:00Z", Value = 25.0, Seq = 1 }
        };

        var result = DataCleaner.Clean(records);

        Assert.Empty(result);
    }

    [Fact]
    public void Clean_NullValue_IsSkipped()
    {
        var records = new List<JsonlRecord>
        {
            new() { DeviceId = "PUMP-01", Metric = "temperature", Ts = "2025-06-01T08:00:00Z", Value = null, Seq = 1 }
        };

        var result = DataCleaner.Clean(records);

        Assert.Empty(result);
    }

    [Fact]
    public void Clean_NullSeq_IsSkipped()
    {
        var records = new List<JsonlRecord>
        {
            new() { DeviceId = "PUMP-01", Metric = "temperature", Ts = "2025-06-01T08:00:00Z", Value = 25.0, Seq = null }
        };

        var result = DataCleaner.Clean(records);

        Assert.Empty(result);
    }

    [Fact]
    public void Clean_DuplicateSeq_KeepsFirstOnly()
    {
        var records = new List<JsonlRecord>
        {
            new() { DeviceId = "PUMP-01", Metric = "temperature", Ts = "2025-06-01T08:00:00Z", Value = 25.0, Seq = 100 },
            new() { DeviceId = "PUMP-01", Metric = "temperature", Ts = "2025-06-01T08:00:00Z", Value = 26.0, Seq = 100 }
        };

        var result = DataCleaner.Clean(records);

        Assert.Single(result);
        Assert.Equal(25.0, result[0].Value);
    }

    [Fact]
    public void Clean_DifferentDevices_KeepsAll()
    {
        var records = new List<JsonlRecord>
        {
            new() { DeviceId = "PUMP-01", Metric = "temperature", Ts = "2025-06-01T08:00:00Z", Value = 25.0, Seq = 100 },
            new() { DeviceId = "PUMP-02", Metric = "temperature", Ts = "2025-06-01T08:00:00Z", Value = 30.0, Seq = 100 }
        };

        var result = DataCleaner.Clean(records);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Clean_OutlierValue_IsFlaggedAsAnomaly()
    {
        var records = new List<JsonlRecord>
        {
            new() { DeviceId = "FAN-03", Metric = "vibration", Ts = "2025-06-01T08:05:00Z", Value = -9999.0, Seq = 100 }
        };

        var result = DataCleaner.Clean(records);

        Assert.Single(result);
        Assert.True(result[0].IsAnomaly);
    }

    [Fact]
    public void Clean_NormalValue_IsNotAnomaly()
    {
        var records = new List<JsonlRecord>
        {
            new() { DeviceId = "PUMP-01", Metric = "temperature", Ts = "2025-06-01T08:00:00Z", Value = 67.0, Seq = 100 }
        };

        var result = DataCleaner.Clean(records);

        Assert.Single(result);
        Assert.False(result[0].IsAnomaly);
    }

    [Fact]
    public void Clean_MultipleRecords_CleansCorrectly()
    {
        var records = new List<JsonlRecord>
        {
            new() { DeviceId = "PUMP-01", Metric = "temperature", Ts = "2025-06-01T08:00:00Z", Value = 67.0, Seq = 100 },
            new() { DeviceId = null, Metric = "temperature", Ts = "2025-06-01T08:00:00Z", Value = 67.0, Seq = 101 },
            new() { DeviceId = "PUMP-01", Metric = "temperature", Ts = "2025-06-01T08:00:00Z", Value = 67.0, Seq = 100 },
            new() { DeviceId = "PUMP-01", Metric = "vibration", Ts = "2025-06-01T08:00:00Z", Value = 1000000.0, Seq = 200 }
        };

        var result = DataCleaner.Clean(records);

        Assert.Equal(2, result.Count);
        Assert.False(result[0].IsAnomaly);
        Assert.True(result[1].IsAnomaly);
    }
}
