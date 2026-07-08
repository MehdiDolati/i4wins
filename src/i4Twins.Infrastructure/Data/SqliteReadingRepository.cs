using Dapper;
using Microsoft.Data.Sqlite;
using i4Twins.Application.Interfaces;
using i4Twins.Domain.Entities;
using i4Twins.Domain.ValueObjects;

namespace i4Twins.Infrastructure.Data;

public class SqliteReadingRepository : IReadingRepository, IDisposable
{
    private readonly string _connectionString;
    private SqliteConnection _connection;
    private readonly List<Reading> _pendingAdds = new();
    private bool _disposed;

    public SqliteReadingRepository(string connectionString)
    {
        _connectionString = connectionString;
        _connection = new SqliteConnection(_connectionString);
        _connection.Open();
    }

    public async Task<bool> ExistsAsync(ReadingIdentity identity, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT COUNT(1) 
            FROM Readings 
            WHERE DeviceId = @DeviceId 
              AND Metric = @Metric 
              AND Timestamp = @Timestamp 
              AND Sequence = @Sequence";

        var count = await _connection.ExecuteScalarAsync<int>(
            sql,
            new
            {
                identity.DeviceId,
                identity.Metric,
                identity.Timestamp,
                identity.Sequence
            });

        return count > 0;
    }

    public async Task AddAsync(Reading reading, CancellationToken cancellationToken = default)
    {
        _pendingAdds.Add(reading);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (!_pendingAdds.Any())
            return;

        const string sql = @"
            INSERT INTO Readings (Id, DeviceId, Metric, Timestamp, Value, Sequence, CreatedAt)
            VALUES (@Id, @DeviceId, @Metric, @Timestamp, @Value, @Sequence, @CreatedAt)";

        using var transaction = _connection.BeginTransaction();
        try
        {
            await _connection.ExecuteAsync(sql, _pendingAdds, transaction);
            await transaction.CommitAsync(cancellationToken);
            _pendingAdds.Clear();
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IEnumerable<Reading>> GetReadingsInRangeAsync(
        string deviceId,
        string metric,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT * FROM Readings 
            WHERE DeviceId = @DeviceId 
              AND Metric = @Metric 
              AND Timestamp >= @From 
              AND Timestamp < @To
            ORDER BY Timestamp";

        return await _connection.QueryAsync<Reading>(
            sql,
            new { DeviceId = deviceId, Metric = metric, From = from, To = to });
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _connection?.Close();
            _connection?.Dispose();
            _connection = null!;
        }

        _disposed = true;
    }
}