using Microsoft.EntityFrameworkCore;
using SWS.Data;
using SWS.Data.Seed;

namespace SWS.Desktop.Services;

/// <summary>
/// Ensures the database exists and is upgraded to the latest schema using EF Core migrations,
/// then seeds required reference data (templates, etc.).
/// </summary>
public static class DatabaseBootstrapper
{
    public static void EnsureDatabaseReady(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Connection string is empty.");

        var options = new DbContextOptionsBuilder<SwsDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        using var db = new SwsDbContext(options);

        // 1) Create DB + apply schema
        db.Database.Migrate();

        // 2) Seed reference data (safe to call repeatedly)
        // IMPORTANT: seeding happens after migrate, using SAME db/context/connection.
        PointTemplateSeeder.SeedIfEmpty(db);
    }
}