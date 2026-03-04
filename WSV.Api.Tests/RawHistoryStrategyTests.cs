using Moq;
using WSV.Api.Models;
using WSV.Api.Services;
using WSV.Api.Services.History;

namespace WSV.Api.Tests;

public class RawHistoryStrategyTests
{ 
    [Fact]
    public async Task RawHistoryGetAsync_WhenNoDataAnywhere_ReturnsEmptyList()
    {
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;

        var context = TestHelpers.CreateContext();

        var mockCache = TestHelpers.CreateMockCache();
        mockCache
            .Setup(c => c.GetRecentOne(11))
            .Returns(new List<SourceReading>());
        
        var rawHistory = new RawHistoryStrategy(context, mockCache.Object);

        var result = await rawHistory.GetAsync(11, timestamp.AddMinutes(-30), timestamp, 10);

        Assert.Empty(result);
    }

    [Fact]
    public async Task RawHistoryGetAsync_WhenDataOnlyInDb_ReturnsDbDataList()
    {
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;
        
        var context = TestHelpers.CreateContext();
        context.SourceReadings.Add(new SourceReading
        {
            SourceId = 11,
            Timestamp = timestamp.AddMinutes(-10)
        });
        await context.SaveChangesAsync();

        var mockCache = TestHelpers.CreateMockCache();
        mockCache
            .Setup(c => c.GetRecentOne(11))
            .Returns(new List<SourceReading>());

        var rawHistory = new RawHistoryStrategy(context, mockCache.Object);

        var result = await rawHistory.GetAsync(11, timestamp.AddMinutes(-30), timestamp, 10);

        Assert.Single(result);
        Assert.Equal(11, result[0].SourceId);
        Assert.Equal(timestamp.AddMinutes(-10), result[0].Timestamp);
    }

    [Fact]
    public async Task RawHistoryGetAsync_WhenDataOnlyInCache_ReturnsCacheDataList()
    {
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;

        var context = TestHelpers.CreateContext();

        var mockCache = TestHelpers.CreateMockCache();
        mockCache
            .Setup(c => c.GetOldestTimestamp(11))
            .Returns(timestamp.AddMinutes(-10));
        mockCache
            .Setup(c => c.GetRecentOne(11))
            .Returns(new List<SourceReading>{
                new SourceReading
                {
                    SourceId = 11,
                    Timestamp = timestamp.AddMinutes(-10)
                }
            });
        
        var rawHistory = new RawHistoryStrategy(context, mockCache.Object);

        var result = await rawHistory.GetAsync(11, timestamp.AddMinutes(-30), timestamp, 10);
    
        Assert.Single(result);
        Assert.Equal(11, result[0].SourceId);
        Assert.Equal(timestamp.AddMinutes(-10), result[0].Timestamp);
    }

    [Fact]
    public async Task RawHistoryGetAsync_WhenAllDataAvailable_ReturnsReadingDtoList()
    {
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;

        var context = TestHelpers.CreateContext();
        context.SourceReadings.Add(new SourceReading
        {
            SourceId = 11,
            Timestamp = timestamp.AddMinutes(-20)
        });
        await context.SaveChangesAsync();

        var mockCache = TestHelpers.CreateMockCache();
        mockCache
            .Setup(c => c.GetOldestTimestamp(11))
            .Returns(timestamp.AddMinutes(-10));
        mockCache
            .Setup(c => c.GetRecentOne(11))
            .Returns(new List<SourceReading>{
                new SourceReading
                {
                    SourceId = 11,
                    Timestamp = timestamp.AddMinutes(-10)
                }
            });
        
        var rawHistory = new RawHistoryStrategy(context, mockCache.Object);

        var result = await rawHistory.GetAsync(11, timestamp.AddMinutes(-30), timestamp, 10);

        Assert.Equal(2, result.Count);
        Assert.Equal(timestamp.AddMinutes(-10), result[0].Timestamp);
        Assert.Equal(timestamp.AddMinutes(-20), result[1].Timestamp);
    }

    [Fact]
    public async Task RawHistoryGetAsync_WhenDataOlderThanFilter_ReturnsEmptyList()
    {
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;

        var context = TestHelpers.CreateContext();
        context.SourceReadings.Add(new SourceReading
        {
            SourceId = 11,
            Timestamp = timestamp.AddMinutes(-40)
        });
        await context.SaveChangesAsync();

        var mockCache = TestHelpers.CreateMockCache();
        mockCache
            .Setup(c => c.GetRecentOne(11))
            .Returns(new List<SourceReading>());

        var rawHistory = new RawHistoryStrategy(context, mockCache.Object);

        var result = await rawHistory.GetAsync(11, timestamp.AddMinutes(-30), timestamp, 10);

        Assert.Empty(result);
    }

    [Fact]
    public async Task RawHistoryGetAsync_WhenDataNewerThanFilter_ReturnsEmptyList()
    {
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;

        var context = TestHelpers.CreateContext();
        context.SourceReadings.Add(new SourceReading
        {
            SourceId = 11,
            Timestamp = timestamp.AddMinutes(+10)
        });
        await context.SaveChangesAsync();

        var mockCache = TestHelpers.CreateMockCache();
        mockCache
            .Setup(c => c.GetRecentOne(11))
            .Returns(new List<SourceReading>());

        var rawHistory = new RawHistoryStrategy(context, mockCache.Object);

        var result = await rawHistory.GetAsync(11, timestamp.AddMinutes(-30), timestamp, 10);

        Assert.Empty(result);
    }

    [Fact]
    public async Task RawHistoryGetAsync_WhenCacheAndDbDataOverlap_ReturnsReadingDtoListWithoutDuplicates()
    {
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;

        var context = TestHelpers.CreateContext();
        context.SourceReadings.AddRange(new SourceReading
        {
            SourceId = 11,
            Timestamp = timestamp.AddMinutes(-15)
        },
        new SourceReading
        {
            SourceId = 11,
            Timestamp = timestamp.AddMinutes(-20)
        });
        await context.SaveChangesAsync();

        var mockCache = TestHelpers.CreateMockCache();
        mockCache
            .Setup(c => c.GetOldestTimestamp(11))
            .Returns(timestamp.AddMinutes(-15));
        mockCache
            .Setup(c => c.GetRecentOne(11))
            .Returns(new List<SourceReading>{
                new SourceReading
                {
                    SourceId = 11,
                    Timestamp = timestamp.AddMinutes(-10)
                },
                new SourceReading
                {
                    SourceId = 11,
                    Timestamp = timestamp.AddMinutes(-15)
                }
            });

        var rawHistory = new RawHistoryStrategy(context, mockCache.Object);

        var result = await rawHistory.GetAsync(11, timestamp.AddMinutes(-30), timestamp, 10);

        Assert.Equal(3, result.Count);
        Assert.Equal(timestamp.AddMinutes(-10), result[0].Timestamp);
        Assert.Equal(timestamp.AddMinutes(-15), result[1].Timestamp);
        Assert.Equal(timestamp.AddMinutes(-20), result[2].Timestamp);
    }
}