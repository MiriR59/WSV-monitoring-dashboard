using WSV.Api.Data;
using WSV.Api.Models;
using Microsoft.EntityFrameworkCore;
using WSV.Api.Services.History;

namespace WSV.Api.Services;

public class ReadingService : IReadingService
{
    private readonly AppDbContext _context;
    private readonly IReadingCacheService _readingCacheService;
    private readonly HistoryStrategySelector _selector;

    public ReadingService(
        AppDbContext context,
        IReadingCacheService readingCacheService,
        HistoryStrategySelector selector)
    {
        _context = context;
        _readingCacheService = readingCacheService;
        _selector = selector;
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

        // Overkill for only 2 GetHistory options, but good OCP application
        var strategy = _selector.Select(count, take);

        return await strategy.GetAsync(sourceId, start, end, take);
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
}