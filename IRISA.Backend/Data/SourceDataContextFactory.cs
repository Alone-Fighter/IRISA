using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace IRISA.Backend.Data;

public sealed class SourceDataContextFactory : IDesignTimeDbContextFactory<SourceDataContext>
{
    public SourceDataContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("SourceConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'SourceConnection' is not configured.");

        var options = new DbContextOptionsBuilder<SourceDataContext>()
            .UseSqlServer(
                connectionString,
                sql => sql.MigrationsHistoryTable("__SourceMigrationsHistory"))
            .Options;

        return new SourceDataContext(options);
    }
}
