using WSV.Api.Models;

namespace WSV.Api.Services;

public interface IReadingService
{
    Task<List<ReadingDto>> GetHistoryAsync(
        int sourceId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int? limit,
        CancellationToken ct = default);

    Task<LagDto> GetLagAsync(int sourceId, CancellationToken ct = default);

    Task<Source?> GetPublicSourceAsync(int sourceId, CancellationToken ct = default);

    Task<Source?> GetSourceAsync(int sourceId, CancellationToken ct = default);
}