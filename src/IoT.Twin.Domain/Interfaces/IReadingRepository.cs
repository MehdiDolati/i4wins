using IoT.Twin.Domain.Entities;

namespace IoT.Twin.Domain.Interfaces;

public interface IReadingRepository
{
    Task AddRangeAsync(IEnumerable<Reading> readings);
    Task<List<string>> GetAllDeviceIdsAsync();
    Task<Reading?> GetLatestAsync(string deviceId, string? metric = null);
    Task<List<Reading>> GetReadingsAsync(string? deviceId, string? metric, DateTime? from, DateTime? to);
    Task<List<Reading>> GetAnomaliesAsync(string? deviceId, string? metric, DateTime? from, DateTime? to);
    Task<bool> ExistsBySequenceAsync(string deviceId, string metric, long sequenceNumber);
}
