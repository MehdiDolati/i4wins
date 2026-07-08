using System.Text.Json;

namespace IoT.Twin.Infrastructure.Services;

public class JsonlRecord
{
    public string? DeviceId { get; set; }
    public string? Metric { get; set; }
    public string? Ts { get; set; }
    public double? Value { get; set; }
    public long? Seq { get; set; }
}

public static class JsonlReader
{
    public static List<JsonlRecord> Read(string filePath)
    {
        var records = new List<JsonlRecord>();
        var lines = File.ReadAllLines(filePath);

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                var record = JsonSerializer.Deserialize<JsonlRecord>(line, options);
                if (record != null)
                    records.Add(record);
            }
            catch (JsonException)
            {
                // Skip malformed JSON lines
            }
        }

        return records;
    }
}
