using WSV.Api.Data;
using Microsoft.EntityFrameworkCore;
using Moq;
using WSV.Api.Services;
using WSV.Api.Services.History;

namespace WSV.Api.Tests;

public static class TestHelpers
{
    public static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("TestDb_" + Guid.NewGuid())
            .Options;
        return new AppDbContext(options);
    }

    public static Mock<IReadingCacheService> CreateMockCache()
    {
        return new Mock<IReadingCacheService>();
    }
    public static Mock<IHistoryStrategySelector> CreateMockSelector()
    {
        return new Mock<IHistoryStrategySelector>();
    }
}