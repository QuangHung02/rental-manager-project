# Release steps

RentalManager currently has two release outputs:

- The existing zip release flow is unchanged. Continue producing and uploading the zip artifact exactly as before.
- The Velopack installer flow is additive and produces a Windows `Setup.exe` installer.

## Build the Velopack installer

Prerequisites:

- Windows 10/11.
- .NET 8 SDK.

Run from the repository root:

```powershell
.\scripts\package-velopack.ps1 -Version 0.2.0
```

The script restores the pinned local `vpk` tool, publishes the WPF app as a self-contained `win-x64` build, and packages it with Velopack.

Output:

```text
release\velopack\RentalManager-win-Setup.exe
```

Velopack also writes its package metadata into `release\velopack`. Upload the installer, `.nupkg`, `RELEASES`, and `releases.win.json` files to the same GitHub Release so installed apps can find updates.

To include release notes in the in-app update dialog, pass a Markdown file when packaging:

```powershell
.\scripts\package-velopack.ps1 -Version 0.2.0 -ReleaseNotesPath .\docs\release-notes\v0.2.0.md
```

## Installer behavior

The generated installer:

- Installs `RentalManager.exe`.
- Creates normal Desktop and Start Menu shortcuts.
- Uses the existing app icon.
- Does not add a backend server.
- Checks GitHub Releases for updates after the app starts.

RentalManager stores its SQLite database under `%LOCALAPPDATA%\RentalManager\rental-manager.sqlite` unless `RENTALMANAGER_DB_PATH` is set. Installing the app to a new location does not move or replace that local data file.

## GitHub Releases update flow

The installed app checks this repository for Velopack releases:

```text
https://github.com/QuangHung02/rental-manager-project
```

Release checklist:

1. Build the Velopack installer with a version higher than the currently published version.
2. Create a GitHub Release for the same version.
3. Upload every file from `release\velopack`, including:
   - `RentalManager-win-Setup.exe`
   - `RentalManager-<version>-full.nupkg`
   - `RELEASES`
   - `releases.win.json`
   - `assets.win.json`
4. Keep the existing zip artifact in the same release if desired. The zip flow is still independent.

When an installed app sees a newer Velopack version, it shows an update dialog with the version number and packaged release notes. If the user chooses **Update now**, the app downloads the update, applies it, and restarts. If GitHub is unavailable or the computer is offline, startup continues normally and no dialog is shown.

## Validation checklist

1. Run `dotnet build RentalManager\RentalManager.sln -c Release`.
2. Run `.\scripts\package-velopack.ps1 -Version <version>`.
3. Confirm `release\velopack\*Setup.exe` exists.
4. Run the installer on Windows.
5. Launch RentalManager from the installer completion screen or installed app.
6. Confirm Desktop and Start Menu shortcuts were created.
7. Confirm existing data still appears, or confirm `%LOCALAPPDATA%\RentalManager\rental-manager.sqlite` is unchanged.
8. With no network connection, launch the installed app and confirm it opens normally without an update dialog.
9. With a newer GitHub Release available, launch the installed app and confirm the update dialog appears.
