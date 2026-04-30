using Microsoft.EntityFrameworkCore;
using System.IO;

namespace RentalManager.Data;

public static class DbContextFactory
{
    public static string DatabasePath { get; } = Environment.GetEnvironmentVariable("RENTALMANAGER_DB_PATH") ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RentalManager",
        "rental-manager.sqlite");

    public static RentalManagerDbContext Create()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath)!);
        var options = new DbContextOptionsBuilder<RentalManagerDbContext>()
            .UseSqlite($"Data Source={DatabasePath}")
            .Options;

        return new RentalManagerDbContext(options);
    }

    public static void EnsureDatabase()
    {
        using var db = Create();
        db.Database.EnsureCreated();
    }
}
