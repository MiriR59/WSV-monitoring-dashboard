namespace WSV.Api.Services.History;

public interface IHistoryStrategy
{
    Task<List<ReadingDto>> GetAsync(
        int sourceId,
        DateTimeOffset from,
        DateTimeOffset to,
        int limit
    );
}