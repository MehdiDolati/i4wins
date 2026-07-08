using i4Twins.Application.DTOs;

namespace i4Twins.Application.Interfaces;

public interface IReadingService
{
    Task<ProcessingReport> ProcessFileAsync(string filePath, CancellationToken cancellationToken = default);
    Task<AggregationResponseDto> GetAggregatedDataAsync(
        string deviceId,
        string metric,
        DateTime from,
        DateTime to,
        int bucketSeconds,
        CancellationToken cancellationToken = default);
}