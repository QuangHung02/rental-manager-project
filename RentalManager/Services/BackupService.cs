using RentalManager.Data;
using Microsoft.Data.Sqlite;
using System.IO;

namespace RentalManager.Services;

public class BackupService
{
    public string DatabasePath => DbContextFactory.DatabasePath;
    public string BackupFolderPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "RentalManagerBackups");

    public string BackupTo(string folderPath, string prefix = "rental-manager-backup")
    {
        Directory.CreateDirectory(folderPath);
        SqliteConnection.ClearAllPools();
        var target = CreateBackupPath(folderPath, prefix);
        File.Copy(DatabasePath, target, false);
        return target;
    }

    public string? RestoreFrom(string backupPath, string? preRestoreFolderPath = null)
    {
        if (!File.Exists(backupPath))
        {
            throw new FileNotFoundException("Không tìm thấy file sao lưu đã chọn.", backupPath);
        }

        if (string.Equals(Path.GetFullPath(backupPath), Path.GetFullPath(DatabasePath), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Không thể khôi phục từ chính file dữ liệu hiện tại.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath)!);
        SqliteConnection.ClearAllPools();
        var preRestoreBackup = File.Exists(DatabasePath)
            ? BackupTo(preRestoreFolderPath ?? BackupFolderPath, "pre-restore-backup")
            : null;

        SqliteConnection.ClearAllPools();
        File.Copy(backupPath, DatabasePath, true);
        SqliteConnection.ClearAllPools();
        return preRestoreBackup;
    }

    private static string CreateBackupPath(string folderPath, string prefix)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd-HHmmss-fff");
        var target = Path.Combine(folderPath, $"{prefix}-{timestamp}.sqlite");
        var counter = 1;
        while (File.Exists(target))
        {
            target = Path.Combine(folderPath, $"{prefix}-{timestamp}-{counter}.sqlite");
            counter++;
        }

        return target;
    }
}
