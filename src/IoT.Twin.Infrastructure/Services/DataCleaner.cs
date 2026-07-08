using IoT.Twin.Domain.Entities;

namespace IoT.Twin.Infrastructure.Services;

public static class DataCleaner
{
    private const double AnomalyThreshold = 1000;

    public static List<Reading> Clean(List<JsonlRecord> records)
    {
        var cleaned = new List<Reading>();
        var seenKeys = new HashSet<(string DeviceId, string Metric, long Seq)>();

        foreach (var record in records)
        {
            // Skip records with null/empty required fields
            if (string.IsNullOrWhiteSpace(record.DeviceId) ||
                string.IsNullOrWhiteSpace(record.Metric) ||
                !record.Value.HasValue ||
                !record.Seq.HasValue)
                continue;

            // Skip duplicates based on (deviceId, metric, sequenceNumber)
            var key = (record.DeviceId, record.Metric, record.Seq.Value);
            if (!seenKeys.Add(key))
                continue;

            if (!DateTime.TryParse(record.Ts, out var timestamp))
                continue;

            cleaned.Add(new Reading
            {
                DeviceId = record.DeviceId,
                Metric = record.Metric,
                Timestamp = timestamp,
                Value = record.Value.Value,
                SequenceNumber = record.Seq.Value,
                IsAnomaly = Math.Abs(record.Value.Value) > AnomalyThreshold
            });
        }

        return cleaned;
    }
}
