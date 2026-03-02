using WSV.Api.Models;

namespace WSV.Api.Services;

public interface IReadingService
{
    Task<List<ReadingDto>> GetHistoryAsync(
        int sourceId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int? limit);
        
    Task<List<ReadingDto>> GetRawHistoryAsync(
        int sourceId,
        DateTimeOffset from,
        DateTimeOffset to);

    Task<List<ReadingDto>> GetAggregatedHistoryAsync(
        int sourceId,
        DateTimeOffset from,
        DateTimeOffset to,
        int limit);
        
    Task<LagDto> GetLagAsync(int sourceId);

    Task<Source?> GetPublicSourceAsync(int sourceId);

    Task<Source?> GetSourceAsync(int sourceId);
}