using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;


namespace SWS.Data;

/// <summary>
/// Used by EF Core tooling (dotnet ef) at design-time.
/// It tells EF how to build SwsDbContext when the app isn't running.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<SwsDbContext>
{
    public SwsDbContext CreateDbContext(string[] args)
    {
        // Tooling runs from various working directories, so we anchor to current directory.
        // When you run `dotnet ef ... --startup-project SWS.Desktop`,
        // EF will typically set the base path to the startup project's output folder.
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)          // if present in current dir
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Expect a connection string named "SwsDb" (same as we used earlier)
        var connectionString = config.GetConnectionString("SwsDb");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            // Fallback: LocalDB (dev-friendly). Change if you're using SQLEXPRESS / server.
            connectionString = @"Server=(localdb)\MSSQLLocalDB;Database=SWS;Trusted_Connection=True;TrustServerCertificate=True";
        }

        var optionsBuilder = new DbContextOptionsBuilder<SwsDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new SwsDbContext(optionsBuilder.Options);
    }
}
