using RentalManager.DTOs;
using Velopack;
using Velopack.Sources;

namespace RentalManager.Services;

public sealed class UpdateService
{
    private const string GitHubRepositoryUrl = "https://github.com/QuangHung02/rental-manager-project";
    private static readonly TimeSpan CheckTimeout = TimeSpan.FromSeconds(20);

    private readonly UpdateManager _updateManager;

    public UpdateService()
    {
        var source = new GithubSource(GitHubRepositoryUrl, null, false, null);
        _updateManager = new UpdateManager(source);
    }

    public async Task<AppUpdateInfo?> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        if (!_updateManager.IsInstalled)
        {
            return null;
        }

        try
        {
            var updateInfo = await _updateManager
                .CheckForUpdatesAsync()
                .WaitAsync(CheckTimeout, cancellationToken);

            return updateInfo is null ? null : new AppUpdateInfo(updateInfo);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task DownloadAndApplyUpdateAsync(
        AppUpdateInfo update,
        Action<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        await _updateManager.DownloadUpdatesAsync(update.UpdateInfo, progress, cancellationToken);
        _updateManager.ApplyUpdatesAndRestart(update.UpdateInfo.TargetFullRelease);
    }
}
