using Microsoft.EntityFrameworkCore;
using WSV.Api.Data;
using WSV.Api.Models;

namespace WSV.Api.Services;

// Background services rungs in the bg and periodically generates
public class GeneratorService : BackgroundService
{
    private readonly ISourceBehaviourService _behaviourService;
    private readonly IReadingCacheService _readingCacheService;
    private readonly IDynamicBufferService _readingBufferService;
    private readonly ISourceCacheService _sourceCacheService;

    public GeneratorService(
        ISourceBehaviourService behaviourService,
        IReadingCacheService readingCacheService,
        IDynamicBufferService readingBufferService,
        ISourceCacheService sourceCacheService)
    {
        _behaviourService = behaviourService;
        _readingCacheService = readingCacheService;
        _readingBufferService = readingBufferService;
        _sourceCacheService = sourceCacheService;
    }

    // Following method is called on the start and runs until shutdown
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Loops until stopped by cancellation
        while (!stoppingToken.IsCancellationRequested)
        {
            // Generate reading for each source and enqueue it
            foreach(var source in _sourceCacheService.GetAllSources())
            {
                var now = DateTimeOffset.UtcNow;
                var reading = _behaviourService.GenerateReading(source, now);

                // Update cache
                _readingCacheService.SetRecentReading(reading);

                // Queue reading in the channel
                _readingBufferService.Enqueue(reading);
            }

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }
}