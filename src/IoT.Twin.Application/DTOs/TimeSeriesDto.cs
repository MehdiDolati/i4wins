namespace IoT.Twin.Application.DTOs;

public record TimeSeriesPoint(
    DateTime Timestamp,
    double Value
);

public record TimeSeriesDto(
    string DeviceId,
    string Metric,
    List<TimeSeriesPoint> Points
);
