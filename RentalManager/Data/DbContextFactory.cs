using Microsoft.EntityFrameworkCore;
using System.Data;
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
        EnsureTenantStatusColumn(db);
    }

    private static void EnsureTenantStatusColumn(RentalManagerDbContext db)
    {
        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
        {
            connection.Open();
        }

        try
        {
            using var readColumns = connection.CreateCommand();
            readColumns.CommandText = "PRAGMA table_info('Tenants');";
            using var reader = readColumns.ExecuteReader();
            while (reader.Read())
            {
                if (string.Equals(reader.GetString(1), "Status", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            using var addColumn = connection.CreateCommand();
            addColumn.CommandText = "ALTER TABLE Tenants ADD COLUMN Status INTEGER NOT NULL DEFAULT 0;";
            addColumn.ExecuteNonQuery();
        }
        finally
        {
            if (shouldClose)
            {
                connection.Close();
            }
        }
    }
}
