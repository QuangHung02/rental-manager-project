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

Velopack also writes its package metadata into `release\velopack`. Keep those files with the installer if you later use Velopack updates, but this project does not enable auto-update yet.

## Installer behavior

The generated installer:

- Installs `RentalManager.exe`.
- Creates normal Desktop and Start Menu shortcuts.
- Uses the existing app icon.
- Does not add a backend server.
- Does not enable auto-update.

RentalManager stores its SQLite database under `%LOCALAPPDATA%\RentalManager\rental-manager.sqlite` unless `RENTALMANAGER_DB_PATH` is set. Installing the app to a new location does not move or replace that local data file.

## Validation checklist

1. Run `dotnet build RentalManager\RentalManager.sln -c Release`.
2. Run `.\scripts\package-velopack.ps1 -Version <version>`.
3. Confirm `release\velopack\*Setup.exe` exists.
4. Run the installer on Windows.
5. Launch RentalManager from the installer completion screen or installed app.
6. Confirm Desktop and Start Menu shortcuts were created.
7. Confirm existing data still appears, or confirm `%LOCALAPPDATA%\RentalManager\rental-manager.sqlite` is unchanged.
