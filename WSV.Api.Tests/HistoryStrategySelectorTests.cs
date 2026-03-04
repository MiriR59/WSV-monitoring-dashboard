
using Moq;
using WSV.Api.Services.History;

namespace WSV.Api.Tests;

public class HistoryStrategySelectorTests
{
    [Fact]
    public void Select_WhenCountBelowLimit_ReturnsRawHistoryStrategy()
    {
        var context = TestHelpers.CreateContext();
        var mockCache = TestHelpers.CreateMockCache();

        var raw = new RawHistoryStrategy(context, mockCache.Object);
        var aggregated = new AggregatedHistoryStrategy(context);
        var selector = new HistoryStrategySelector(raw, aggregated);

        var result = selector.Select(count: 111, limit: 222);

        Assert.IsType<RawHistoryStrategy>(result);
    }

    [Fact]
    public void Select_WhenCountAboveLimit_ReturnsAggreagtedHistoryStrategy()
    {
        var context = TestHelpers.CreateContext();
        var mockCache = TestHelpers.CreateMockCache();

        var raw = new RawHistoryStrategy(context, mockCache.Object);
        var aggregated = new AggregatedHistoryStrategy(context);
        var selector = new HistoryStrategySelector(raw, aggregated);

        var result = selector.Select(count: 222, limit: 111);

        Assert.IsType<AggregatedHistoryStrategy>(result);
    }
}