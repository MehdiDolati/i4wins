using System.Text.Json.Serialization;

namespace i4Twins.Application.DTOs;

public class ReadingDto
{
    [JsonPropertyName("deviceId")]
    public string DeviceId { get; set; } = string.Empty;

    [JsonPropertyName("metric")]
    public string Metric { get; set; } = string.Empty;

    [JsonPropertyName("ts")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("value")]
    public double Value { get; set; }

    [JsonPropertyName("seq")]
    public int Sequence { get; set; }
}