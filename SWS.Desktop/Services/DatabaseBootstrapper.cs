using Microsoft.EntityFrameworkCore;
using SWS.Data;

namespace SWS.Desktop.Services;

/// <summary>
/// Ensures the database exists and is upgraded to the latest schema using EF Core migrations.
/// </summary>
public static class DatabaseBootstrapper
{
    public static void EnsureDatabaseMigrated(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Connection string is empty.");

        var options = new DbContextOptionsBuilder<SwsDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        using var db = new SwsDbContext(options);
        db.Database.Migrate(); // ✅ creates DB if missing, applies migrations if pending
    }
}
