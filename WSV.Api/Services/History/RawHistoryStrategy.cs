using WSV.Api.Data;
using WSV.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace WSV.Api.Services.History;

public class RawHistoryStrategy : IHistoryStrategy
{
    private readonly AppDbContext _context;

    private readonly IReadingCacheService _readingCacheService;

    public RawHistoryStrategy(
        AppDbContext context,
        IReadingCacheService readingCacheService)
    {
        _context = context;
        _readingCacheService = readingCacheService;
    }
    public async Task<List<ReadingDto>> GetAsync(
        int sourceId,
        DateTimeOffset from,
        DateTimeOffset to,
        int limit)
    {
        IEnumerable<SourceReading> cacheFiltered = Enumerable.Empty<SourceReading>();
        IEnumerable<SourceReading> readings = Enumerable.Empty<SourceReading>();

        var cacheOldest = _readingCacheService.GetOldestTimestamp(sourceId);

        if(cacheOldest != null && cacheOldest < to)
        {
            var cacheReadings = _readingCacheService.GetRecentOne(sourceId);
            cacheFiltered = cacheReadings
                .Where(r => r.Timestamp < to)
                .Where(r => r.Timestamp >= from);
        }

        bool needDb = cacheOldest == null || cacheOldest > from;
        if(needDb)
        {
            readings = await _context.SourceReadings
                .AsNoTracking()
                .Where(t => t.SourceId == sourceId)
                .Where(u => u.Timestamp >= from)
                .Where(v => v.Timestamp < to)
                .OrderByDescending(r => r.Timestamp)  
                .ToListAsync();
        }

        var merged = new Dictionary<DateTimeOffset, ReadingDto>();
        foreach (var d in cacheFiltered.Select(MapToDto))
            merged[d.Timestamp] = d;
        foreach (var d in readings.Select(MapToDto))
            merged[d.Timestamp] = d;

        return merged.Values
            .OrderByDescending(r => r.Timestamp)
            .ToList();
    }

    private static ReadingDto MapToDto(SourceReading reading) => new ReadingDto
    {
        SourceId = reading.SourceId,
        Timestamp = reading.Timestamp,
        Status = reading.Status,
        RPM = reading.RPM,
        Power = reading.Power,
        Temperature = reading.Temperature
    };
}