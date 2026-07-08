namespace IoT.Twin.Application.DTOs;

public record AggregationDto(
    string DeviceId,
    string Metric,
    int Count,
    double Min,
    double Max,
    double Average
);
