namespace IoT.Twin.Application.DTOs;

public record AnomalyDto(
    string DeviceId,
    string Metric,
    DateTime Timestamp,
    double Value,
    long SequenceNumber,
    string Reason
);
