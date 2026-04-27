using RentalManager.Data;
using System.IO;

namespace RentalManager.Services;

public class BackupService
{
    public string DatabasePath => DbContextFactory.DatabasePath;

    public string BackupTo(string folderPath)
    {
        Directory.CreateDirectory(folderPath);
        var target = Path.Combine(folderPath, $"rental-manager-backup-{DateTime.Today:yyyy-MM-dd}.sqlite");
        File.Copy(DatabasePath, target, true);
        return target;
    }

    public void RestoreFrom(string backupPath)
    {
        File.Copy(backupPath, DatabasePath, true);
    }
}
