using Microsoft.EntityFrameworkCore;
using Moq;
using WSV.Api.Data;
using WSV.Api.Models;
using WSV.Api.Services;
using WSV.Api.Services.History;
using Xunit.Sdk;

namespace WSV.Api.Tests;

public class ReadingServiceTests
{
    [Fact]
    public async Task GetLagAsync_WhenNoLiveData_ReturnsNoLiveDataState()
    {
        // Create empty in-memory db - no readings needed for this
        var context = TestHelpers.CreateContext();

        // --- ARANGE --- set up everything the service needs
        // Create fake cache that returns null for any sourceId
        var mockCache = TestHelpers.CreateMockCache();
        mockCache
            .Setup(c => c.GetLatestOne(11))
            .Returns((SourceReading?)null);

        var mockSelector = TestHelpers.CreateMockSelector();
        
        // Wire up the service with fake dependencies
        var service = new ReadingService(context, mockCache.Object, mockSelector.Object);

        // --- ACT --- call the method we are testing
        var result = await service.GetLagAsync(11);

        // --- ASSERT --- verify the result against what we expect
        Assert.Equal(LagState.NoLiveData, result.State);
        Assert.Equal(11, result.SourceId);
    }

    [Fact]
    public async Task GetLagAsync_WhenNoDbData_ReturnsDbEmptyState()
    {
        var context = TestHelpers.CreateContext();

        var fakeReading = new SourceReading
        {
            SourceId = 11,
            Timestamp = DateTimeOffset.Now
        };

        var mockCache = TestHelpers.CreateMockCache();
        mockCache
            .Setup(c => c.GetLatestOne(11))
            .Returns(fakeReading);
        
        var mockSelector = TestHelpers.CreateMockSelector();
        
        var service = new ReadingService(context, mockCache.Object, mockSelector.Object);

        var result = await service.GetLagAsync(11);

        Assert.Equal(LagState.DbEmpty, result.State);
        Assert.Equal(fakeReading.Timestamp, result.LatestGenerated);
    }

    [Fact]
    public async Task GetLagAsync_WhenAllDataAvailable_ReturnsLagDto()
    {
        DateTimeOffset timestampUnited = DateTimeOffset.UtcNow;

        var context = TestHelpers.CreateContext();
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

        var mockCache = TestHelpers.CreateMockCache();
        mockCache
            .Setup(c => c.GetLatestOne(11))
            .Returns(fakeReading);
        
        var mockSelector = TestHelpers.CreateMockSelector();
        
        var service = new ReadingService(context, mockCache.Object, mockSelector.Object);

        var result = await service.GetLagAsync(11);

        Assert.Equal(LagState.Ok, result.State);
        Assert.Equal(10, result.DbLag);
    }

    [Fact]
    public async Task GetPublicSourceAsync_WhenIdDoesNotMatchAndIsPublicIsTrue_ReturnsNull()
    {
        var context = TestHelpers.CreateContext();
        context.Sources.Add(new Source
        {
            Id = 11,
            Name = "Source11",
            IsPublic = true
        });
        await context.SaveChangesAsync();

        var mockCache = TestHelpers.CreateMockCache();

        var mockSelector = TestHelpers.CreateMockSelector();
        
        var service = new ReadingService(context, mockCache.Object, mockSelector.Object);

        var result = await service.GetPublicSourceAsync(22);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetPublicSourceAsync_WhenIdMatchesAndIsPublicIsFalse_ReturnsNull()
    {
        var context = TestHelpers.CreateContext();
        context.Sources.Add(new Source
        {
            Id = 11,
            Name = "Source11",
            IsPublic = false
        });
        await context.SaveChangesAsync();

        var mockCache = TestHelpers.CreateMockCache();

        var mockSelector = TestHelpers.CreateMockSelector();
        
        var service = new ReadingService(context, mockCache.Object, mockSelector.Object);

        var result = await service.GetPublicSourceAsync(11);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetPublicSourceAsync_WhenIdMatchesAndIsPublicIsTrue_ReturnsSource()
    {
        var context = TestHelpers.CreateContext();
        context.Sources.Add(new Source
        {
            Id = 11,
            Name = "Source11",
            IsPublic = true
        });
        await context.SaveChangesAsync();

        var mockCache = TestHelpers.CreateMockCache();

        var mockSelector = TestHelpers.CreateMockSelector();
        
        var service = new ReadingService(context, mockCache.Object, mockSelector.Object);

        var result = await service.GetPublicSourceAsync(11);

        Assert.NotNull(result);
        Assert.Equal("Source11", result.Name);
    }

    [Fact]
    public async Task GetSourceAsync_WhenSourceDoesNotExist_ReturnsNull()
    {
        var context = TestHelpers.CreateContext();
        context.Sources.Add(new Source
        {
            Id = 11,
            Name = "Source11"
        });
        await context.SaveChangesAsync();

        var mockCache = TestHelpers.CreateMockCache();

        var mockSelector = TestHelpers.CreateMockSelector();
        
        var service = new ReadingService(context, mockCache.Object, mockSelector.Object);

        var result = await service.GetSourceAsync(22);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetSourceAsync_WhenSourceExists_ReturnsSource()
    {
        var context = TestHelpers.CreateContext();
        context.Sources.Add(new Source
        {
            Id = 11,
            Name = "Source11"
        });
        await context.SaveChangesAsync();

        var mockCache = TestHelpers.CreateMockCache();

        var mockSelector = TestHelpers.CreateMockSelector();
        
        var service = new ReadingService(context, mockCache.Object, mockSelector.Object);

        var result = await service.GetSourceAsync(11);

        Assert.NotNull(result);
        Assert.Equal("Source11", result.Name);
    }

}
