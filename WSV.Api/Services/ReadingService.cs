using WSV.Api.Data;
using WSV.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace WSV.Api.Services;

public class ReadingService : IReadingService
{
    private readonly AppDbContext _context;
    private readonly IReadingCacheService _readingCacheService;

    public ReadingService(
        AppDbContext context,
        IReadingCacheService readingCacheService)
    {
        _context = context;
        _readingCacheService = readingCacheService;
    }

    public async Task<List<ReadingDto>> GetHistoryAsync(int sourceId, DateTimeOffset? from, DateTimeOffset? to, int? limit)
    {
        var start = from ?? DateTimeOffset.UtcNow.AddDays(-1);
        var end = to ?? DateTimeOffset.UtcNow;
        var take = Math.Clamp(limit ?? 1000, 1, 5000);

        var query = _context.SourceReadings
            .AsNoTracking()
            .Where(t => t.SourceId == sourceId)
            .Where(u => u.Timestamp >= start)
            .Where(v => v.Timestamp < end);
            
        var count = await query.CountAsync();

        if(count > take)
            return await GetAggregatedHistoryAsync(sourceId, start, end, take);
        
        return await GetRawHistoryAsync(sourceId, start, end);
    }

    public async Task<List<ReadingDto>> GetRawHistoryAsync(int sourceId, DateTimeOffset from, DateTimeOffset to)
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

    public async Task<List<ReadingDto>> GetAggregatedHistoryAsync(int sourceId, DateTimeOffset from, DateTimeOffset to, int limit)
    {
        var spanSeconds = (to - from).TotalSeconds;
        var bucketSeconds = (int)(spanSeconds / limit);

        var sql = @"
            SELECT
                {0} as ""SourceId"",
                TO_TIMESTAMP(
                    FLOOR(EXTRACT(EPOCH FROM ""Timestamp"") / {1}) *{1}
                ) as ""Timestamp"",
                'Aggregated' as ""Status"",
                CAST(AVG(""RPM"") AS INTEGER) as ""RPM"",
                CAST(AVG(""Power"") AS INTEGER) as ""Power"",
                AVG(""Temperature"") as ""Temperature""
            FROM ""SourceReadings""
            WHERE ""SourceId"" = {0}
            AND ""Timestamp"" >= {2}
            AND ""Timestamp"" < {3}
            GROUP BY FLOOR(EXTRACT(EPOCH FROM ""Timestamp"") / {1})
            ORDER BY ""Timestamp""";

        var results = await _context.Database
            .SqlQueryRaw<ReadingDto>(sql,
                sourceId,
                bucketSeconds,
                from,
                to)
            .ToListAsync();

        return results;
    }

    public async Task<LagDto> GetLagAsync(int sourceId)
    {
        var latestGenerated = _readingCacheService.GetLatestOne(sourceId);
        if(latestGenerated is null)
            return new LagDto{
                SourceId = sourceId,
                State = LagState.NoLiveData};

        var latestDb = await _context.SourceReadings
            .AsNoTracking()
            .Where(r => r.SourceId == sourceId)
            .MaxAsync(r => (DateTimeOffset?)r.Timestamp);

        if(latestDb is null)
            return new LagDto{
                SourceId = sourceId,
                State = LagState.DbEmpty,
                LatestGenerated = latestGenerated.Timestamp};

        var lag = latestGenerated.Timestamp - latestDb.Value;
        var lagOut = Math.Max(0, lag.TotalSeconds);

        return new LagDto{
            SourceId = sourceId,
            State = LagState.Ok,
            LatestGenerated = latestGenerated.Timestamp,
            LatestDb = latestDb.Value,
            DbLag = lagOut};
    }

    public async Task<Source?> GetPublicSourceAsync(int sourceId)
    {
        return await _context.Sources
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == sourceId && p.IsPublic);
    }

    public async Task<Source?> GetSourceAsync(int sourceId)
    {
        return await _context.Sources
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == sourceId);
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