#Requires -Version 5.1
<#
.SYNOPSIS
    Buduje release ZGRZYT Desktop (self-contained folder + ZIP).

.DESCRIPTION
    Nie commituje artefaktow. ZIP i folder publish sa ignorowane przez .gitignore.
#>
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $RepoRoot

$Project = Join-Path $RepoRoot "ZgrzytDesktop\ZgrzytDesktop.csproj"
$PublishDir = Join-Path $RepoRoot "ZgrzytDesktop\bin\Release\net10.0-windows\win-x64\publish"
$ReadmeRelease = Join-Path $RepoRoot "README_RELEASE.txt"
$ZipPath = Join-Path $RepoRoot "ZgrzytDesktop-win-x64-release.zip"

function Stop-BlockingProcesses {
    Get-Process -Name "ZgrzytDesktop" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | ForEach-Object {
        try {
            if ($_.Path -and $_.Path.StartsWith($RepoRoot, [StringComparison]::OrdinalIgnoreCase)) {
                Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
            }
        }
        catch {
            # Ignoruj procesy bez sciezki.
        }
    }
    Start-Sleep -Seconds 1
}

Write-Host "==> Zatrzymywanie procesow blokujacych pliki..."
Stop-BlockingProcesses

Write-Host "==> dotnet clean"
dotnet clean

Write-Host "==> dotnet restore"
dotnet restore

Write-Host "==> dotnet build"
dotnet build --no-restore

Write-Host "==> dotnet test (bez integracji live API)"
dotnet test --no-build --filter "Category!=Integration"

Write-Host "==> dotnet publish (self-contained folder)"
dotnet publish $Project `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:PublishTrimmed=false

if (-not (Test-Path $PublishDir)) {
    throw "Brak folderu publish: $PublishDir"
}

if (-not (Test-Path $ReadmeRelease)) {
    throw "Brak README_RELEASE.txt w katalogu glownym repozytorium."
}

Write-Host "==> Kopiowanie README_RELEASE.txt do publish"
Copy-Item -Path $ReadmeRelease -Destination (Join-Path $PublishDir "README_RELEASE.txt") -Force

if (Test-Path $ZipPath) {
    Remove-Item $ZipPath -Force
}

Write-Host "==> Tworzenie ZIP: $ZipPath"
Compress-Archive -Path (Join-Path $PublishDir "*") -DestinationPath $ZipPath -Force

$exePath = Join-Path $PublishDir "ZgrzytDesktop.exe"
if (-not (Test-Path $exePath)) {
    throw "Brak ZgrzytDesktop.exe w publish."
}

Write-Host ""
Write-Host "Release gotowy."
Write-Host "  EXE:    $exePath"
Write-Host "  Folder: $PublishDir"
Write-Host "  ZIP:    $ZipPath"
Write-Host ""
Write-Host "ZIP nie jest commitowany (patrz .gitignore: *.zip)."
