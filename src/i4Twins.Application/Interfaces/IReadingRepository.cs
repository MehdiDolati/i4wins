using i4Twins.Domain.Entities;
using i4Twins.Domain.ValueObjects;

namespace i4Twins.Application.Interfaces;

public interface IReadingRepository
{
    Task<bool> ExistsAsync(ReadingIdentity identity, CancellationToken cancellationToken = default);
    Task AddAsync(Reading reading, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Reading>> GetReadingsInRangeAsync(
        string deviceId,
        string metric,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);
}