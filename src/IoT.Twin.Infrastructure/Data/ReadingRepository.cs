using IoT.Twin.Domain.Entities;
using IoT.Twin.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IoT.Twin.Infrastructure.Data;

public class ReadingRepository : IReadingRepository
{
    private readonly AppDbContext _db;

    public ReadingRepository(AppDbContext db) => _db = db;

    public async Task AddRangeAsync(IEnumerable<Reading> readings)
    {
        await _db.Readings.AddRangeAsync(readings);
        await _db.SaveChangesAsync();
    }

    public async Task<List<string>> GetAllDeviceIdsAsync()
    {
        return await _db.Readings
            .Select(r => r.DeviceId)
            .Distinct()
            .OrderBy(d => d)
            .ToListAsync();
    }

    public async Task<Reading?> GetLatestAsync(string deviceId, string? metric = null)
    {
        var query = _db.Readings.Where(r => r.DeviceId == deviceId);
        if (!string.IsNullOrEmpty(metric))
            query = query.Where(r => r.Metric == metric);

        return await query
            .OrderByDescending(r => r.Timestamp)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Reading>> GetReadingsAsync(
        string? deviceId, string? metric, DateTime? from, DateTime? to)
    {
        var query = _db.Readings.AsQueryable();

        if (!string.IsNullOrEmpty(deviceId))
            query = query.Where(r => r.DeviceId == deviceId);
        if (!string.IsNullOrEmpty(metric))
            query = query.Where(r => r.Metric == metric);
        if (from.HasValue)
            query = query.Where(r => r.Timestamp >= from.Value);
        if (to.HasValue)
            query = query.Where(r => r.Timestamp <= to.Value);

        return await query.OrderBy(r => r.Timestamp).ToListAsync();
    }

    public async Task<List<Reading>> GetAnomaliesAsync(
        string? deviceId, string? metric, DateTime? from, DateTime? to)
    {
        var query = _db.Readings.Where(r => r.IsAnomaly);

        if (!string.IsNullOrEmpty(deviceId))
            query = query.Where(r => r.DeviceId == deviceId);
        if (!string.IsNullOrEmpty(metric))
            query = query.Where(r => r.Metric == metric);
        if (from.HasValue)
            query = query.Where(r => r.Timestamp >= from.Value);
        if (to.HasValue)
            query = query.Where(r => r.Timestamp <= to.Value);

        return await query.OrderBy(r => r.Timestamp).ToListAsync();
    }

    public async Task<bool> ExistsBySequenceAsync(string deviceId, string metric, long sequenceNumber)
    {
        return await _db.Readings.AnyAsync(r =>
            r.DeviceId == deviceId &&
            r.Metric == metric &&
            r.SequenceNumber == sequenceNumber);
    }
}
