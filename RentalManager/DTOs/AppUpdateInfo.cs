using Velopack;

namespace RentalManager.DTOs;

public sealed class AppUpdateInfo
{
    public AppUpdateInfo(UpdateInfo updateInfo)
    {
        UpdateInfo = updateInfo;
        Version = updateInfo.TargetFullRelease.Version.ToString();
        ReleaseNotes = updateInfo.TargetFullRelease.NotesMarkdown;
    }

    public UpdateInfo UpdateInfo { get; }

    public string Version { get; }

    public string? ReleaseNotes { get; }
}
