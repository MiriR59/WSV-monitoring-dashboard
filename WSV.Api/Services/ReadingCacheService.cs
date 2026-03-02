using Microsoft.Extensions.Options;
using WSV.Api.Models;
using WSV.Api.Configuration;

namespace WSV.Api.Services;

public class ReadingCacheService : IReadingCacheService
{
    private readonly ILogger<ReadingCacheService> _logger;
    private readonly TimeSpan _retention;
    private readonly int _warningThreshold;
    private readonly Dictionary<int, Queue<SourceReading>> _cache = new();
    private readonly Dictionary<int, SourceReading> _latest = new();
    private readonly object _lock = new();

    public ReadingCacheService(
        ILogger<ReadingCacheService> logger,
        IOptions<CacheOptions> options)
    {
        _logger = logger;

        var opt = options.Value;
        _retention = opt.Retention;
        _warningThreshold = opt.WarningThreshold;

        _logger.LogInformation("ReadingCacheService initialized with {retention} second retention.", _retention.TotalSeconds);
    }

    public IReadOnlyList<SourceReading> GetRecentOne(int sourceId)
    {
        lock (_lock)
        {
            ExpireOldReadingsLock(sourceId, DateTimeOffset.UtcNow);

            if (_cache.TryGetValue(sourceId, out var q) && q.Count > 0)
                return q.ToList();

            return Array.Empty<SourceReading>();
        }
    }

    public SourceReading? GetLatestOne(int sourceId)
    {
        lock (_lock)
        {
            return _latest.TryGetValue(sourceId, out var r) ? r : null;
        }
    }
    public void SetRecentReading(SourceReading reading)
    {
        lock (_lock)
        {
            var now = DateTimeOffset.UtcNow;

            _latest[reading.SourceId] = reading;

            if (!_cache.TryGetValue(reading.SourceId, out var q))
            {
                q = new Queue<SourceReading>();
                _cache[reading.SourceId] = q;
            }

            q.Enqueue(reading);

            if (q.Count == 1)
            {
                _logger.LogInformation("Started caching data for source {SourceId}", reading.SourceId);
            }

            if (q.Count > _warningThreshold)
            {
                _logger.LogWarning("Cache for source {SourceId} has {Count} - retention may be too high.", reading.SourceId, q.Count);
            }

            ExpireQueueLock(q, now, _retention);
        }
    }

    private void ExpireOldReadingsLock(int sourceId, DateTimeOffset now)
    {
        if (!_cache.TryGetValue(sourceId, out var q))
            return;

        ExpireQueueLock(q, now, _retention);

        if (q.Count == 0)
            _cache.Remove(sourceId);
    }

    private static void ExpireQueueLock(Queue<SourceReading> q, DateTimeOffset now, TimeSpan retention)
    {
        var cutoff = now - retention;

        while (q.Count > 0)
        {
            var oldest = q.Peek();
            if (oldest.Timestamp >= cutoff)
                break;

            q.Dequeue();
        }
    }

    public DateTimeOffset? GetOldestTimestamp(int sourceId)
    {
        lock (_lock)
        {
            ExpireOldReadingsLock(sourceId, DateTimeOffset.UtcNow);

            if(_cache.TryGetValue(sourceId, out var q) && q.Count> 0)
                return q.Peek().Timestamp;
            
            return null;
        }
    }
}   