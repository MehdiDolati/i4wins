namespace i4Twins.Application.DTOs;

public class AggregationResponseDto
{
    public string DeviceId { get; set; } = string.Empty;
    public string Metric { get; set; } = string.Empty;
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public int BucketSizeSeconds { get; set; }
    public List<AggregationBucketDto> Buckets { get; set; } = new();
}

public class AggregationBucketDto
{
    public DateTime BucketStart { get; set; }
    public int Count { get; set; }
    public double Average { get; set; }
    public double Minimum { get; set; }
    public double Maximum { get; set; }
}