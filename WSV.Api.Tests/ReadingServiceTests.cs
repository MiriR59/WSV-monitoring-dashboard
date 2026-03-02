using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Moq;
using WSV.Api.Data;
using WSV.Api.Models;
using WSV.Api.Services;
using Xunit.Sdk;

namespace WSV.Api.Tests;

public class ReadingServiceTests
{
    // Helper that creates fresh in-memory DB for each test
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("TestDb_" + Guid.NewGuid())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetRawHistoryAsync_WhenNoDataAnywhere_ReturnsEmptyList()
    {
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;

        var context = CreateContext();

        var mockCache = new Mock<IReadingCacheService>();
        mockCache
            .Setup(c => c.GetRecentOne(11))
            .Returns(new List<SourceReading>());
            
        var service = new ReadingService(context, mockCache.Object);

        var result = await service.GetRawHistoryAsync(11, timestamp.AddMinutes(-30), timestamp);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetRawHistoryAsync_WhenDataOnlyInDb_ReturnsDbDataList()
    {
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;
        
        var context = CreateContext();
        context.SourceReadings.Add(new SourceReading
        {
            SourceId = 11,
            Timestamp = timestamp.AddMinutes(-10)
        });
        await context.SaveChangesAsync();

        var mockCache = new Mock<IReadingCacheService>();
        mockCache
            .Setup(c => c.GetRecentOne(11))
            .Returns(new List<SourceReading>());

        var service = new ReadingService(context, mockCache.Object);

        var result = await service.GetRawHistoryAsync(11, timestamp.AddMinutes(-30), timestamp);

        Assert.Single(result);
        Assert.Equal(11, result[0].SourceId);
        Assert.Equal(timestamp.AddMinutes(-10), result[0].Timestamp);
    }

    [Fact]
    public async Task GetRawHistoryAsync_WhenDataOnlyInCache_ReturnsCacheDataList()
    {
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;

        var context = CreateContext();

        var mockCache = new Mock<IReadingCacheService>();
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
        
        var service = new ReadingService(context, mockCache.Object);

        var result = await service.GetRawHistoryAsync(11, timestamp.AddMinutes(-30), timestamp);
    
        Assert.Single(result);
        Assert.Equal(11, result[0].SourceId);
        Assert.Equal(timestamp.AddMinutes(-10), result[0].Timestamp);
    }

    [Fact]
    public async Task GetRawHistoryAsync_WhenAllDataAvailable_ReturnsReadingDtoList()
    {
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;

        var context = CreateContext();
        context.SourceReadings.Add(new SourceReading
        {
            SourceId = 11,
            Timestamp = timestamp.AddMinutes(-20)
        });
        await context.SaveChangesAsync();

        var mockCache = new Mock<IReadingCacheService>();
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
        
        var service = new ReadingService(context, mockCache.Object);

        var result = await service.GetRawHistoryAsync(11, timestamp.AddMinutes(-30), timestamp);

        Assert.Equal(2, result.Count);
        Assert.Equal(timestamp.AddMinutes(-10), result[0].Timestamp);
        Assert.Equal(timestamp.AddMinutes(-20), result[1].Timestamp);
    }

    [Fact]
    public async Task GetRawHistoryAsync_WhenDataOlderThanFilter_ReturnsEmptyList()
    {
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;

        var context = CreateContext();
        context.SourceReadings.Add(new SourceReading
        {
            SourceId = 11,
            Timestamp = timestamp.AddMinutes(-40)
        });
        await context.SaveChangesAsync();

        var mockCache = new Mock<IReadingCacheService>();
        mockCache
            .Setup(c => c.GetRecentOne(11))
            .Returns(new List<SourceReading>());

        var service = new ReadingService(context, mockCache.Object);

        var result = await service.GetRawHistoryAsync(11, timestamp.AddMinutes(-30), timestamp);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetRawHistoryAsync_WhenDataNewerThanFilter_ReturnsEmptyList()
    {
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;

        var context = CreateContext();
        context.SourceReadings.Add(new SourceReading
        {
            SourceId = 11,
            Timestamp = timestamp.AddMinutes(+10)
        });
        await context.SaveChangesAsync();

        var mockCache = new Mock<IReadingCacheService>();
        mockCache
            .Setup(c => c.GetRecentOne(11))
            .Returns(new List<SourceReading>());

        var service = new ReadingService(context, mockCache.Object);

        var result = await service.GetRawHistoryAsync(11, timestamp.AddMinutes(-30), timestamp);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetRawHistoryAsync_WhenCacheAndDbDataOverlap_ReturnsReadingDtoListWithoutDuplicates()
    {
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;

        var context = CreateContext();
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

        var mockCache = new Mock<IReadingCacheService>();
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
        
        var service = new ReadingService(context, mockCache.Object);

        var result = await service.GetRawHistoryAsync(11, timestamp.AddMinutes(-30), timestamp);

        Assert.Equal(3, result.Count);
        Assert.Equal(timestamp.AddMinutes(-10), result[0].Timestamp);
        Assert.Equal(timestamp.AddMinutes(-15), result[1].Timestamp);
        Assert.Equal(timestamp.AddMinutes(-20), result[2].Timestamp);
    }

    [Fact]
    public async Task GetLagAsync_WhenNoLiveData_ReturnsNoLiveDataState()
    {
        // Create empty in-memory db - no readings needed for this
        var context = CreateContext();

        // --- ARANGE --- set up everything the service needs
        // Create fake cache that returns null for any sourceId
        var mockCache = new Mock<IReadingCacheService>();
        mockCache
            .Setup(c => c.GetLatestOne(11))
            .Returns((SourceReading?)null);

        // Wire up the service with fake dependencies
        var service = new ReadingService(context, mockCache.Object);

        // --- ACT --- call the method we are testing
        var result = await service.GetLagAsync(11);

        // --- ASSERT --- verify the result against what we expect
        Assert.Equal(LagState.NoLiveData, result.State);
        Assert.Equal(11, result.SourceId);
    }

    [Fact]
    public async Task GetLagAsync_WhenNoDbData_ReturnsDbEmptyState()
    {
        var context = CreateContext();

        var fakeReading = new SourceReading
        {
            SourceId = 11,
            Timestamp = DateTimeOffset.Now
        };

        var mockCache = new Mock<IReadingCacheService>();
        mockCache
            .Setup(c => c.GetLatestOne(11))
            .Returns(fakeReading);
        
        var service = new ReadingService(context, mockCache.Object);

        var result = await service.GetLagAsync(11);

        Assert.Equal(LagState.DbEmpty, result.State);
        Assert.Equal(fakeReading.Timestamp, result.LatestGenerated);
    }

    [Fact]
    public async Task GetLagAsync_WhenAllDataAvailable_ReturnsLagDto()
    {
        DateTimeOffset timestampUnited = DateTimeOffset.UtcNow;

        var context = CreateContext();
        context.SourceReadings.Add(new SourceReading
        {
            SourceId = 11,
            Timestamp = timestampUnited.AddSeconds(-10)
        });
        await context.SaveChangesAsync();

        var fakeReading = new SourceReading
        {
            SourceId = 11,
            Timestamp = timestampUnited
        };

        var mockCache = new Mock<IReadingCacheService>();
        mockCache
            .Setup(c => c.GetLatestOne(11))
            .Returns(fakeReading);
        
        var service = new ReadingService(context, mockCache.Object);

        var result = await service.GetLagAsync(11);

        Assert.Equal(LagState.Ok, result.State);
        Assert.Equal(10, result.DbLag);
    }

    [Fact]
    public async Task GetPublicSourceAsync_WhenIdDoesNotMatchAndIsPublicIsTrue_ReturnsNull()
    {
        var context = CreateContext();
        context.Sources.Add(new Source
        {
            Id = 11,
            Name = "Source11",
            IsPublic = true
        });
        await context.SaveChangesAsync();

        var mockCache = new Mock<IReadingCacheService>();

        var service = new ReadingService(context, mockCache.Object);

        var result = await service.GetPublicSourceAsync(22);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetPublicSourceAsync_WhenIdMatchesAndIsPublicIsFalse_ReturnsNull()
    {
        var context = CreateContext();
        context.Sources.Add(new Source
        {
            Id = 11,
            Name = "Source11",
            IsPublic = false
        });
        await context.SaveChangesAsync();

        var mockCache = new Mock<IReadingCacheService>();

        var service = new ReadingService(context, mockCache.Object);

        var result = await service.GetPublicSourceAsync(11);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetPublicSourceAsync_WhenIdMatchesAndIsPublicIsTrue_ReturnsSource()
    {
        var context = CreateContext();
        context.Sources.Add(new Source
        {
            Id = 11,
            Name = "Source11",
            IsPublic = true
        });
        await context.SaveChangesAsync();

        var mockCache = new Mock<IReadingCacheService>();

        var service = new ReadingService(context, mockCache.Object);

        var result = await service.GetPublicSourceAsync(11);

        Assert.NotNull(result);
        Assert.Equal("Source11", result.Name);
    }

    [Fact]
    public async Task GetSourceAsync_WhenSourceDoesNotExist_ReturnsNull()
    {
        var context = CreateContext();
        context.Sources.Add(new Source
        {
            Id = 11,
            Name = "Source11"
        });
        await context.SaveChangesAsync();

        var mockCache = new Mock<IReadingCacheService>();

        var service = new ReadingService(context, mockCache.Object);

        var result = await service.GetSourceAsync(22);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetSourceAsync_WhenSourceExists_ReturnsSource()
    {
        var context = CreateContext();
        context.Sources.Add(new Source
        {
            Id = 11,
            Name = "Source11"
        });
        await context.SaveChangesAsync();

        var mockCache = new Mock<IReadingCacheService>();

        var service = new ReadingService(context, mockCache.Object);

        var result = await service.GetSourceAsync(11);

        Assert.NotNull(result);
        Assert.Equal("Source11", result.Name);
    }

}
