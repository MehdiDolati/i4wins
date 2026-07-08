namespace IoT.Twin.Domain.Entities;

public class Reading
{
    public int Id { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string Metric { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
    public long SequenceNumber { get; set; }
    public bool IsAnomaly { get; set; }
}
