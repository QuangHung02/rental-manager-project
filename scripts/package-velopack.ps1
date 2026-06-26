param(
    [string]$Version = "",
    [string]$Runtime = "win-x64",
    [string]$Configuration = "Release",
    [string]$OutputDir = "release\velopack",
    [string]$ReleaseNotesPath = ""
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$projectPath = Join-Path $repoRoot "RentalManager\RentalManager.csproj"
$iconPath = Join-Path $repoRoot "RentalManager\Assets\appicon.ico"
$publishDir = Join-Path $repoRoot "artifacts\velopack\publish-$Runtime"
$releaseDir = Join-Path $repoRoot $OutputDir

function Invoke-Checked {
    param(
        [string]$FilePath,
        [string[]]$Arguments
    )

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$FilePath failed with exit code $LASTEXITCODE."
    }
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    [xml]$projectXml = Get-Content $projectPath
    $projectVersion = $projectXml.Project.PropertyGroup |
        ForEach-Object { $_.Version } |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        Select-Object -First 1

    $Version = if ([string]::IsNullOrWhiteSpace($projectVersion)) { "0.2.0" } else { $projectVersion }
}

$Version = $Version.Trim().TrimStart([char[]]@("v", "V"))

Write-Host "Packaging RentalManager $Version for $Runtime..."

if (Test-Path $publishDir) {
    Remove-Item -LiteralPath $publishDir -Recurse -Force
}

if (Test-Path $releaseDir) {
    Remove-Item -LiteralPath $releaseDir -Recurse -Force
}

New-Item -ItemType Directory -Path $publishDir -Force | Out-Null
New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null

Invoke-Checked "dotnet" @("tool", "restore")

Invoke-Checked "dotnet" @(
    "publish",
    $projectPath,
    "-c",
    $Configuration,
    "-r",
    $Runtime,
    "--self-contained",
    "true",
    "-o",
    $publishDir,
    "-p:Version=$Version"
)

$packArgs = @(
    "tool",
    "run",
    "vpk",
    "--",
    "pack",
    "--packId",
    "RentalManager",
    "--packVersion",
    $Version,
    "--packDir",
    $publishDir,
    "--mainExe",
    "RentalManager.exe",
    "--packTitle",
    "RentalManager",
    "--packAuthors",
    "RentalManager",
    "--icon",
    $iconPath,
    "--runtime",
    $Runtime,
    "--shortcuts",
    "Desktop,StartMenuRoot",
    "--noPortable",
    "--outputDir",
    $releaseDir
)

if (-not [string]::IsNullOrWhiteSpace($ReleaseNotesPath)) {
    $resolvedReleaseNotesPath = Resolve-Path $ReleaseNotesPath
    $packArgs += @("--releaseNotes", $resolvedReleaseNotesPath)
}

Invoke-Checked "dotnet" $packArgs

$setupPath = Join-Path $releaseDir "RentalManager-win-Setup.exe"
if (-not (Test-Path $setupPath)) {
    $setupPath = Get-ChildItem -Path $releaseDir -Filter "*Setup.exe" | Select-Object -First 1 -ExpandProperty FullName
}

if ([string]::IsNullOrWhiteSpace($setupPath) -or -not (Test-Path $setupPath)) {
    throw "Velopack packaging completed but no Setup.exe was found in $releaseDir."
}

Write-Host "Created installer: $setupPath"
