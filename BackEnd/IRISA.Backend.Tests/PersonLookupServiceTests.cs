using IRISA.Backend.Data;
using IRISA.Backend.Models;
using IRISA.Backend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace IRISA.Backend.Tests;

public sealed class PersonLookupServiceTests
{
    [Fact]
    public async Task FindAsync_ReadsSourceAndStoresSnapshot()
    {
        await using var databases = await TestDatabases.CreateAsync();
        var service = databases.CreateService();

        var result = await service.FindAsync("1111111111", CancellationToken.None);

        Assert.False(result.SourceUnavailable);
        Assert.NotNull(result.Person);
        Assert.False(result.Person.IsStale);
        Assert.Equal("source", result.Person.DataSource);
        Assert.Equal("آزمایشی الف", result.Person.FullName);
        Assert.Single(result.Person.LabResults);
        Assert.Equal(1, await databases.Application.PersonSnapshots.CountAsync());
    }

    [Fact]
    public async Task FindAsync_ReturnsCachedSnapshotWhenSourceIsUnavailable()
    {
        await using var databases = await TestDatabases.CreateAsync();
        var service = databases.CreateService();
        await service.FindAsync("1111111111", CancellationToken.None);
        databases.Configuration["DemoData:SimulateSourceUnavailable"] = "true";

        var result = await service.FindAsync("1111111111", CancellationToken.None);

        Assert.True(result.SourceUnavailable);
        Assert.NotNull(result.Person);
        Assert.True(result.Person.IsStale);
        Assert.Equal("cache", result.Person.DataSource);
        Assert.Single(result.Person.LabResults);
    }

    [Fact]
    public async Task FindAsync_ReportsUnavailableWhenSourceAndCacheAreBothMissing()
    {
        await using var databases = await TestDatabases.CreateAsync();
        databases.Configuration["DemoData:SimulateSourceUnavailable"] = "true";

        var result = await databases.CreateService()
            .FindAsync("9999999999", CancellationToken.None);

        Assert.True(result.SourceUnavailable);
        Assert.Null(result.Person);
    }

    [Fact]
    public async Task FindAsync_DistinguishesNotFoundFromSourceOutage()
    {
        await using var databases = await TestDatabases.CreateAsync();

        var result = await databases.CreateService()
            .FindAsync("9999999999", CancellationToken.None);

        Assert.False(result.SourceUnavailable);
        Assert.Null(result.Person);
    }

    [Fact]
    public async Task RefreshFromSourceAsync_UpdatesCachedPersonAndResults()
    {
        await using var databases = await TestDatabases.CreateAsync();
        var service = databases.CreateService();
        await service.FindAsync("1111111111", CancellationToken.None);

        var sourcePerson = await databases.Source.People
            .Include(person => person.LabResults)
            .SingleAsync(person => person.NationalCode == "1111111111");
        sourcePerson.FullName = "سارا احمدی به‌روزشده";
        sourcePerson.LabResults.Single().ResultValue = "95";
        await databases.Source.SaveChangesAsync();

        var result = await service.RefreshFromSourceAsync(
            "1111111111",
            CancellationToken.None);

        Assert.False(result.SourceUnavailable);
        Assert.NotNull(result.Person);
        Assert.False(result.Person.IsStale);
        Assert.Equal("سارا احمدی به‌روزشده", result.Person.FullName);
        Assert.Equal("95", Assert.Single(result.Person.LabResults).ResultValue);
    }

    [Fact]
    public async Task RefreshFromSourceAsync_ReturnsCachedDataWhenSourceIsUnavailable()
    {
        await using var databases = await TestDatabases.CreateAsync();
        var service = databases.CreateService();
        await service.FindAsync("1111111111", CancellationToken.None);
        databases.Configuration["DemoData:SimulateSourceUnavailable"] = "true";

        var result = await service.RefreshFromSourceAsync(
            "1111111111",
            CancellationToken.None);

        Assert.True(result.SourceUnavailable);
        Assert.NotNull(result.Person);
        Assert.True(result.Person.IsStale);
        Assert.Equal("cache", result.Person.DataSource);
    }

    private sealed class TestDatabases : IAsyncDisposable
    {
        private TestDatabases(
            SourceDataContext source,
            ApplicationDbContext application,
            IConfigurationRoot configuration)
        {
            Source = source;
            Application = application;
            Configuration = configuration;
        }

        public SourceDataContext Source { get; }

        public ApplicationDbContext Application { get; }

        public IConfigurationRoot Configuration { get; }

        public static async Task<TestDatabases> CreateAsync()
        {
            var sourceOptions = new DbContextOptionsBuilder<SourceDataContext>()
                .UseInMemoryDatabase($"source-{Guid.NewGuid()}")
                .Options;
            var applicationOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"application-{Guid.NewGuid()}")
                .Options;
            var source = new SourceDataContext(sourceOptions);
            var application = new ApplicationDbContext(applicationOptions);

            source.People.Add(new SourcePerson
            {
                Id = Guid.NewGuid(),
                NationalCode = "1111111111",
                FullName = "آزمایشی الف",
                UpdatedAtUtc = DateTimeOffset.UtcNow,
                LabResults =
                [
                    new SourceLabResult
                    {
                        Id = Guid.NewGuid(),
                        TestName = "قند خون",
                        ResultValue = "92",
                        Unit = "mg/dL",
                        ReferenceRange = "70 - 99",
                        PerformedAtUtc = DateTimeOffset.UtcNow
                    }
                ]
            });
            await source.SaveChangesAsync();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["DemoData:SimulateSourceUnavailable"] = "false"
                })
                .Build();

            return new TestDatabases(source, application, configuration);
        }

        public PersonLookupService CreateService()
        {
            return new PersonLookupService(
                Source,
                Application,
                Configuration,
                NullLogger<PersonLookupService>.Instance);
        }

        public async ValueTask DisposeAsync()
        {
            await Source.DisposeAsync();
            await Application.DisposeAsync();
        }
    }
}
