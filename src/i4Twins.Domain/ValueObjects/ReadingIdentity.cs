namespace i4Twins.Domain.ValueObjects;

public record ReadingIdentity(
    string DeviceId,
    string Metric,
    DateTime Timestamp,
    int Sequence
);