namespace WSV.Api.Services.History;

public interface IHistoryStrategySelector
{
    IHistoryStrategy Select(int count, int limit);
}