using i4Twins.Domain.Exceptions;
using i4Twins.Domain.ValueObjects;

namespace i4Twins.Domain.Entities;

public class Reading
{
    public Guid Id { get; private set; }
    public string DeviceId { get; private set; } = string.Empty;
    public string Metric { get; private set; } = string.Empty;
    public DateTime Timestamp { get; private set; }
    public double Value { get; private set; }
    public int Sequence { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public ReadingIdentity Identity => new(DeviceId, Metric, Timestamp, Sequence);

    private Reading() { }

    public static Reading Create(string deviceId, string metric, DateTime timestamp, double value, int sequence)
    {
        Validate(deviceId, metric, timestamp, value, sequence);

        return new Reading
        {
            Id = Guid.NewGuid(),
            DeviceId = deviceId.Trim(),
            Metric = metric.Trim().ToLowerInvariant(),
            Timestamp = timestamp.ToUniversalTime(),
            Value = value,
            Sequence = sequence,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static void Validate(string deviceId, string metric, DateTime timestamp, double value, int sequence)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new InvalidReadingException("DeviceId cannot be empty");

        if (string.IsNullOrWhiteSpace(metric))
            throw new InvalidReadingException("Metric cannot be empty");

        if (timestamp > DateTime.UtcNow.AddDays(1))
            throw new InvalidReadingException($"Timestamp {timestamp:O} is in the future");

        if (timestamp.Year < 2000 || timestamp.Year > 2100)
            throw new InvalidReadingException($"Timestamp {timestamp:O} is out of valid range");

        if (double.IsNaN(value) || double.IsInfinity(value))
            throw new InvalidReadingException($"Value {value} is not a valid number");

        if (value < -10000 || value > 100000)
            throw new InvalidReadingException($"Value {value} is outside reasonable range");

        if (sequence <= 0)
            throw new InvalidReadingException($"Sequence {sequence} must be positive");

        // Physical range validation
        var metricLower = metric.ToLowerInvariant();
        if (metricLower == "temperature" && (value < -50 || value > 150))
            throw new InvalidReadingException($"Temperature {value} is outside physical range (-50 to 150)");

        if (metricLower == "pressure" && (value < 0 || value > 100))
            throw new InvalidReadingException($"Pressure {value} is outside physical range (0 to 100)");

        if (metricLower == "vibration" && (value < -10 || value > 20))
            throw new InvalidReadingException($"Vibration {value} is outside physical range (-10 to 20)");
    }
}