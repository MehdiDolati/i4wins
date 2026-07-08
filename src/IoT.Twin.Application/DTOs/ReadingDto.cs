namespace IoT.Twin.Application.DTOs;

public record ReadingDto(
    string DeviceId,
    string Metric,
    DateTime Timestamp,
    double Value,
    long SequenceNumber,
    bool IsAnomaly
);
