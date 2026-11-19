#Requires -Version 5.1
<#
.SYNOPSIS
    Build STTech.BytesIO Release NuGet packages to Install folder.
.DESCRIPTION
    Builds and packs STTech.BytesIO core and extension libraries.
    Outputs .nupkg to Install folder.
#>

[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string]$OutputDir
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

$projects = @(
    (Join-Path $repoRoot 'STTech.BytesIO\STTech.BytesIO.csproj'),
    (Join-Path $repoRoot 'STTech.BytesIO.Bluetooth\STTech.BytesIO.Bluetooth.csproj'),
    (Join-Path $repoRoot 'STTech.BytesIO.Modbus\STTech.BytesIO.Modbus.csproj'),
    (Join-Path $repoRoot 'STTech.BytesIO.Mqtt\STTech.BytesIO.Mqtt.csproj'),
    (Join-Path $repoRoot 'STTech.BytesIO.P2P\STTech.BytesIO.P2P.csproj'),
    (Join-Path $repoRoot 'STTech.BytesIO.Quic\STTech.BytesIO.Quic.csproj'),
    (Join-Path $repoRoot 'STTech.BytesIO.SerialPortStream\STTech.BytesIO.SerialPortStream.csproj')
)

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $outDir = Join-Path $repoRoot 'Install'
} else {
    if ([System.IO.Path]::IsPathRooted($OutputDir)) {
        $outDir = $OutputDir
    } else {
        $outDir = [System.IO.Path]::GetFullPath((Join-Path (Get-Location) $OutputDir))
    }
}

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) {
    throw "dotnet CLI not found. Please install .NET SDK."
}

foreach ($proj in $projects) {
    if (-not (Test-Path -LiteralPath $proj)) {
        throw "Project file not found: $proj"
    }
}

New-Item -ItemType Directory -Path $outDir -Force | Out-Null

Get-ChildItem -Path $outDir -Filter '*.nupkg' -File -ErrorAction SilentlyContinue |
    Remove-Item -Force

Write-Host "==> Packing STTech.BytesIO Release NuGet packages..." -ForegroundColor Cyan
Write-Host "    Output directory: $outDir" -ForegroundColor Gray

foreach ($proj in $projects) {
    Write-Host "`n--> Packing project: $proj" -ForegroundColor Cyan
    & $dotnet.Path pack $proj -c Release -o $outDir --nologo -p:GeneratePackageOnBuild=false
    if ($LASTEXITCODE -ne 0) {
        throw "Project $proj pack failed with exit code: $LASTEXITCODE"
    }
}

$packages = @(Get-ChildItem -Path $outDir -Filter '*.nupkg' -File)
if ($packages.Count -eq 0) {
    throw "Packing completed but no .nupkg files found."
}

Write-Host "`n==> Success! Generated $($packages.Count) NuGet packages:" -ForegroundColor Green
$packages | ForEach-Object {
    $sizeKB = [Math]::Round($_.Length / 1KB, 1)
    Write-Host ("    {0}  ({1} KB)" -f $_.Name, $sizeKB) -ForegroundColor Green
}
