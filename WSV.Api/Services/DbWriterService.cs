using Microsoft.Extensions.Options;
using WSV.Api.Data;
using WSV.Api.Models;
using WSV.Api.Configuration;

namespace WSV.Api.Services;

public class DbWriterService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDynamicBufferService _buffer;
    private readonly ILogger<DbWriterService> _logger;

    private readonly TimeSpan _tickInterval;
    private readonly int _normalBatch;
    private readonly int _slowBatch;

    private static readonly TimeSpan CycleLength = TimeSpan.FromMinutes(6);
    private static readonly TimeSpan SlowDuration = TimeSpan.FromMinutes(3);

    private readonly DateTime _startUtc = DateTime.UtcNow;

    public DbWriterService(
        IServiceScopeFactory scopeFactory,
        IDynamicBufferService buffer,
        ILogger<DbWriterService> logger,
        IOptions<WriterOptions> options)
    {
        _scopeFactory = scopeFactory;
        _buffer = buffer;
        _logger = logger;

        var opt = options.Value;
        _tickInterval = opt.TickInterval;
        _normalBatch = opt.NormalBatch;
        _slowBatch = opt.SlowBatch;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_tickInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var batchSize = IsSlowMode(DateTime.UtcNow) ? _slowBatch : _normalBatch;

            var batch = new List<SourceReading>(capacity: batchSize);
           
            for (int i = 0; i < batchSize; i++)
            {
                var reading = _buffer.Dequeue();
                if(reading is not null)
                    batch.Add(reading);
                else
                    break;
            }

            if (batch.Count == 0)
                continue;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                db.SourceReadings.AddRange(batch);
                await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                // Error logging
                _logger.LogError(ex, "DB writer failed. Lost {Count} readings from {from} to {to}.",
                    batch.Count, batch.First().Timestamp, batch.Last().Timestamp);
                continue;
            }

            _logger.LogInformation("DB writer tick: target={Target}, wrote={Wrote}, buffered={Buffered}, mode={Mode}",
                batchSize, batch.Count, _buffer.BufferedCount, batchSize == _slowBatch ? "SLOW" : "NORMAL");
        }
    }
    // %  returns what remains after the division, slowmode at the start of each cycle
    private bool IsSlowMode(DateTime nowUtc)
    {
        var elapsed = nowUtc - _startUtc;
        var withinCycle = TimeSpan.FromSeconds(elapsed.TotalSeconds % CycleLength.TotalSeconds);
        return withinCycle < SlowDuration;
    }
}