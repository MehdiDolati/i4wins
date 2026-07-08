using IoT.Twin.Application.DTOs;
using IoT.Twin.Domain.Interfaces;

namespace IoT.Twin.Application.Services;

public class ReadingService
{
    private readonly IReadingRepository _repository;

    public ReadingService(IReadingRepository repository) => _repository = repository;

    public async Task<List<string>> GetDeviceIdsAsync()
        => await _repository.GetAllDeviceIdsAsync();

    public async Task<ReadingDto?> GetLatestAsync(string deviceId, string? metric = null)
    {
        var reading = await _repository.GetLatestAsync(deviceId, metric);
        return reading == null ? null : MapToDto(reading);
    }

    private static ReadingDto MapToDto(Domain.Entities.Reading r)
        => new(r.DeviceId, r.Metric, r.Timestamp, r.Value, r.SequenceNumber, r.IsAnomaly);
}
