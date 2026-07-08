using System.Text.Json;
using Microsoft.Extensions.Logging;
using i4Twins.Application.DTOs;
using i4Twins.Application.Interfaces;
using i4Twins.Domain.Entities;
using i4Twins.Domain.Exceptions;

namespace i4Twins.Application.Services;

public class ReadingService : IReadingService
{
    private readonly IReadingRepository _repository;
    private readonly ILogger<ReadingService> _logger;

    public ReadingService(IReadingRepository repository, ILogger<ReadingService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ProcessingReport> ProcessFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting file processing: {FilePath}", filePath);
        var report = new ProcessingReport();

        if (!File.Exists(filePath))
        {
            _logger.LogError("File not found: {FilePath}", filePath);
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        var lines = await File.ReadAllLinesAsync(filePath, cancellationToken);
        report.TotalLines = lines.Length;
        _logger.LogInformation("Read {LineCount} lines from file", lines.Length);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            try
            {
                var readingDto = JsonSerializer.Deserialize<ReadingDto>(line, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (readingDto == null)
                {
                    report.InvalidRecords++;
                    _logger.LogWarning("Failed to deserialize line: {Line}", line);
                    continue;
                }

                var reading = Reading.Create(
                    readingDto.DeviceId,
                    readingDto.Metric,
                    readingDto.Timestamp,
                    readingDto.Value,
                    readingDto.Sequence
                );

                if (await _repository.ExistsAsync(reading.Identity, cancellationToken))
                {
                    report.DuplicatesSkipped++;
                    _logger.LogDebug("Duplicate skipped: {Identity}", reading.Identity);
                    continue;
                }

                await _repository.AddAsync(reading, cancellationToken);
                report.StoredCount++;
                _logger.LogTrace("Stored reading: {Identity}", reading.Identity);
            }
            catch (InvalidReadingException ex)
            {
                report.InvalidRecords++;
                _logger.LogWarning("Invalid record - {Error}: {Line}", ex.Message, line);
            }
            catch (JsonException ex)
            {
                report.InvalidRecords++;
                _logger.LogWarning("JSON parsing error - {Error}: {Line}", ex.Message, line);
            }
            catch (Exception ex)
            {
                report.InvalidRecords++;
                _logger.LogError(ex, "Unexpected error processing line: {Line}", line);
            }
        }

        await _repository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Processing completed. Report: {Report}", report);

        return report;
    }

    public async Task<AggregationResponseDto> GetAggregatedDataAsync(
        string deviceId,
        string metric,
        DateTime from,
        DateTime to,
        int bucketSeconds,
        CancellationToken cancellationToken = default)
    {
        if (from >= to)
            throw new ArgumentException("'from' must be earlier than 'to'");

        if (bucketSeconds <= 0)
            throw new ArgumentException("Bucket size must be positive");

        _logger.LogInformation(
            "Getting aggregation for {DeviceId}/{Metric} from {From} to {To} with {Bucket}s buckets",
            deviceId, metric, from, to, bucketSeconds);

        var readings = await _repository.GetReadingsInRangeAsync(
            deviceId, metric, from, to, cancellationToken);

        var bucketList = readings.ToList();
        _logger.LogDebug("Found {Count} readings in range", bucketList.Count);

        var buckets = new List<AggregationBucketDto>();
        var current = from;

        while (current < to)
        {
            var bucketEnd = current.AddSeconds(bucketSeconds);
            var inBucket = bucketList
                .Where(r => r.Timestamp >= current && r.Timestamp < bucketEnd)
                .ToList();

            if (inBucket.Any())
            {
                buckets.Add(new AggregationBucketDto
                {
                    BucketStart = current,
                    Count = inBucket.Count,
                    Average = Math.Round(inBucket.Average(r => r.Value), 3),
                    Minimum = Math.Round(inBucket.Min(r => r.Value), 3),
                    Maximum = Math.Round(inBucket.Max(r => r.Value), 3)
                });
            }
            // Empty buckets are omitted as documented in README

            current = bucketEnd;
        }

        _logger.LogInformation("Returning {BucketCount} buckets", buckets.Count);

        return new AggregationResponseDto
        {
            DeviceId = deviceId,
            Metric = metric,
            From = from,
            To = to,
            BucketSizeSeconds = bucketSeconds,
            Buckets = buckets
        };
    }
}