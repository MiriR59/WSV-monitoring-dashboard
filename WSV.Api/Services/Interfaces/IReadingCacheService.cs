using WSV.Api.Models;

namespace WSV.Api.Services;

public interface IReadingCacheService
{
    IReadOnlyList<SourceReading> GetRecentOne(int sourceId);

    SourceReading? GetLatestOne(int sourceId);

    void SetRecentReading(SourceReading reading);

    DateTimeOffset? GetOldestTimestamp(int sourceId); 
}