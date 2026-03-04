namespace WSV.Api.Services.History;

public class HistoryStrategySelector : IHistoryStrategySelector
{
    private readonly RawHistoryStrategy _raw;
    private readonly AggregatedHistoryStrategy _aggregated;

    public HistoryStrategySelector(
        RawHistoryStrategy raw,
        AggregatedHistoryStrategy aggregated)
    {
        _raw = raw;
        _aggregated = aggregated;
    }

    public IHistoryStrategy Select(int count, int limit)
    {
        if(count > limit)
            return _aggregated;
        else
            return _raw;
    }
}