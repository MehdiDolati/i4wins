using IoT.Twin.Application.DTOs;
using IoT.Twin.Domain.Interfaces;

namespace IoT.Twin.Application.Services;

public class ReportService
{
    private readonly IReadingRepository _repository;

    public ReportService(IReadingRepository repository) => _repository = repository;

    public async Task<List<AggregationDto>> GetAggregationAsync(
        string? deviceId, string? metric, DateTime? from, DateTime? to)
    {
        var readings = await _repository.GetReadingsAsync(deviceId, metric, from, to);

        return readings
            .GroupBy(r => new { r.DeviceId, r.Metric })
            .Select(g => new AggregationDto(
                g.Key.DeviceId,
                g.Key.Metric,
                g.Count(),
                g.Min(r => r.Value),
                g.Max(r => r.Value),
                g.Average(r => r.Value)
            ))
            .ToList();
    }

    public async Task<List<TimeSeriesDto>> GetTimeSeriesAsync(
        string? deviceId, string? metric, DateTime? from, DateTime? to)
    {
        var readings = await _repository.GetReadingsAsync(deviceId, metric, from, to);

        return readings
            .GroupBy(r => new { r.DeviceId, r.Metric })
            .Select(g => new TimeSeriesDto(
                g.Key.DeviceId,
                g.Key.Metric,
                g.OrderBy(r => r.Timestamp)
                  .Select(r => new TimeSeriesPoint(r.Timestamp, r.Value))
                  .ToList()
            ))
            .ToList();
    }

    public async Task<List<AnomalyDto>> GetAnomaliesAsync(
        string? deviceId, string? metric, DateTime? from, DateTime? to)
    {
        var readings = await _repository.GetAnomaliesAsync(deviceId, metric, from, to);

        return readings.Select(r => new AnomalyDto(
            r.DeviceId,
            r.Metric,
            r.Timestamp,
            r.Value,
            r.SequenceNumber,
            $"Value {r.Value} exceeds threshold for {r.Metric}"
        )).ToList();
    }
}
