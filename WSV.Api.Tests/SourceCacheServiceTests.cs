using Microsoft.Extensions.DependencyInjection;
using Moq;
using WSV.Api.Data;
using WSV.Api.Models;
using WSV.Api.Services;

namespace WSV.Api.Tests;

public class SourceCacheServiceTests
{
    [Fact]
    public void GetAllSources_WhenCacheEmpty_ReturnsEmptyList()
    {
        var fakeScope = new Mock<IServiceScopeFactory>();
        var service = new SourceCacheService(fakeScope.Object);

        var result = service.GetAllSources();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllSources_WhenCacheHasSource_ReturnsSourcesList()
    {
        var context = TestHelpers.CreateContext();
        context.Sources.Add(new Source
        {
            Name = "Source"
        });
        await context.SaveChangesAsync();

        var fakeProvider = new Mock<IServiceProvider>();
        fakeProvider
            .Setup(c => c.GetService(typeof(AppDbContext)))
            .Returns(context);

         var fakeScope = new Mock<IServiceScope>();
        fakeScope
            .Setup(c => c.ServiceProvider)
            .Returns(fakeProvider.Object);

        var fakeScopeFactory = new Mock<IServiceScopeFactory>();
        fakeScopeFactory
            .Setup(c => c.CreateScope())
            .Returns(fakeScope.Object);

        var service = new SourceCacheService(fakeScopeFactory.Object);

        await service.ReloadSourcesAsync();

        var result = service.GetAllSources();

        Assert.Single(result);
        Assert.Equal("Source", result[0].Name);
    }
        
    [Fact]
    public async Task GetAllSources_ReturnsCopy_NotOriginalList()
    {
        var context = TestHelpers.CreateContext();
        context.Sources.Add(new Source
        {
            Name = "Source"
        });
        await context.SaveChangesAsync();

        var fakeProvider = new Mock<IServiceProvider>();
        fakeProvider
            .Setup(c => c.GetService(typeof(AppDbContext)))
            .Returns(context);

         var fakeScope = new Mock<IServiceScope>();
        fakeScope
            .Setup(c => c.ServiceProvider)
            .Returns(fakeProvider.Object);

        var fakeScopeFactory = new Mock<IServiceScopeFactory>();
        fakeScopeFactory
            .Setup(c => c.CreateScope())
            .Returns(fakeScope.Object);

        var service = new SourceCacheService(fakeScopeFactory.Object);

        await service.ReloadSourcesAsync();

        var result11 = service.GetAllSources();
        var result22 = service.GetAllSources();

        // Make sure the result is the copy, not original leaked
        Assert.NotSame(result11, result22);
        Assert.Equal(result11, result22);
    }

    [Fact]
    public async Task ReloadSourceAsync_WhenCalled_UpdatesCache()
    {
        var context = TestHelpers.CreateContext();
        context.Sources.Add(new Source
        {
            Name = "Source_A"
        });
        await context.SaveChangesAsync();

        var fakeProvider = new Mock<IServiceProvider>();
        fakeProvider
            .Setup(c => c.GetService(typeof(AppDbContext)))
            .Returns(context);

        var fakeScope = new Mock<IServiceScope>();
        fakeScope
            .Setup(c => c.ServiceProvider)
            .Returns(fakeProvider.Object);

        var fakeScopeFactory = new Mock<IServiceScopeFactory>();
        fakeScopeFactory
            .Setup(c => c.CreateScope())
            .Returns(fakeScope.Object);

        var service = new SourceCacheService(fakeScopeFactory.Object);

        await service.ReloadSourcesAsync();

        var result11 = service.GetAllSources();

        Assert.Single(result11);
        Assert.Equal("Source_A", result11[0].Name);

        context.Sources.Add(new Source
        {
            Name = "Source_B"
        });
        await context.SaveChangesAsync();

        await service.ReloadSourcesAsync();

        var result22 = service.GetAllSources();

        Assert.Equal(2, result22.Count);
        Assert.Equal("Source_A", result22[0].Name);
        Assert.Equal("Source_B", result22[1].Name);
    }

    [Fact]
    public async Task ReloadSourceAsync_WhenCalled_OverwritesCache()
    {
        var context = TestHelpers.CreateContext();
        context.Sources.Add(new Source
        {
            Name = "Source_A"
        });
        await context.SaveChangesAsync();

        var fakeProvider = new Mock<IServiceProvider>();
        fakeProvider
            .Setup(c => c.GetService(typeof(AppDbContext)))
            .Returns(context);

        var fakeScope = new Mock<IServiceScope>();
        fakeScope
            .Setup(c => c.ServiceProvider)
            .Returns(fakeProvider.Object);

        var fakeScopeFactory = new Mock<IServiceScopeFactory>();
        fakeScopeFactory
            .Setup(c => c.CreateScope())
            .Returns(fakeScope.Object);

        var service = new SourceCacheService(fakeScopeFactory.Object);

        await service.ReloadSourcesAsync();

        var result11 = service.GetAllSources();

        Assert.Single(result11);
        Assert.Equal("Source_A", result11[0].Name);

        var sourceA = context.Sources.First();
        context.Sources.Remove(sourceA);
        context.Sources.Add(new Source
        {
            Name = "Source_B"
        });
        await context.SaveChangesAsync();

        await service.ReloadSourcesAsync();
        
        var result22 = service.GetAllSources();

        Assert.Single(result22);
        Assert.Equal("Source_B", result22[0].Name);
    }
}