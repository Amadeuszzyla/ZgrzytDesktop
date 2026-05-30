#Requires -Version 5.1
<#
.SYNOPSIS
    Buduje produkcyjny release ZGRZYT Desktop (self-contained folder + ZIP + SHA256).

.DESCRIPTION
    Flow: clean -> restore -> build (Release) -> test (bez integracji) -> publish win-x64 -> ZIP -> checksum.
    Artefakty trafiają do ./release/ i nie są commitowane (patrz .gitignore).
#>
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$Configuration = "Release"
$Runtime = "win-x64"

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $RepoRoot

$Project = Join-Path $RepoRoot "ZgrzytDesktop\ZgrzytDesktop.csproj"
$PublishDir = Join-Path $RepoRoot "ZgrzytDesktop\bin\$Configuration\net10.0-windows\$Runtime\publish"
$ReleaseDir = Join-Path $RepoRoot "release"
$ZipName = "ZgrzytDesktop-win-x64-release.zip"
$ZipPath = Join-Path $ReleaseDir $ZipName
$ChecksumPath = "$ZipPath.sha256"
$WriteReadmeScript = Join-Path $RepoRoot "scripts\write-publish-readme.ps1"

function Write-Step {
    param([Parameter(Mandatory)][string] $Message)
    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Write-StepOk {
    param([Parameter(Mandatory)][string] $Message)
    Write-Host "    OK: $Message" -ForegroundColor Green
}

function Invoke-BuildStep {
    param(
        [Parameter(Mandatory)][string] $Label,
        [Parameter(Mandatory)][scriptblock] $Command
    )

    Write-Step $Label
    & $Command

    if ($LASTEXITCODE -ne 0) {
        throw "Krok nie powiódł się: $Label (kod wyjścia: $LASTEXITCODE)."
    }
}

function Stop-BlockingProcesses {
    Write-Host "    Sprawdzanie procesow blokujacych pliki..." -ForegroundColor DarkGray

    Get-Process -Name "ZgrzytDesktop" -ErrorAction SilentlyContinue |
        Stop-Process -Force -ErrorAction SilentlyContinue

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

function New-ReleaseDirectory {
    if (-not (Test-Path $ReleaseDir)) {
        New-Item -ItemType Directory -Path $ReleaseDir | Out-Null
    }
}

function Write-Sha256Checksum {
    param(
        [Parameter(Mandatory)][string] $FilePath,
        [Parameter(Mandatory)][string] $OutputPath
    )

    if (-not (Test-Path $FilePath)) {
        throw "Brak pliku do checksumy: $FilePath"
    }

    $hash = (Get-FileHash -Path $FilePath -Algorithm SHA256).Hash.ToLowerInvariant()
    $fileName = [IO.Path]::GetFileName($FilePath)
    $line = "{0}  {1}" -f $hash, $fileName

    [IO.File]::WriteAllText($OutputPath, $line, [Text.UTF8Encoding]::new($false))
}

Write-Host "ZGRZYT Desktop — publish release ($Configuration / $Runtime)" -ForegroundColor White
Write-Host "Repozytorium: $RepoRoot" -ForegroundColor DarkGray

Write-Step "Zatrzymywanie procesow blokujacych pliki"
Stop-BlockingProcesses
Write-StepOk "Procesy zatrzymane"

Invoke-BuildStep "dotnet clean (-c $Configuration)" {
    dotnet clean -c $Configuration
}

Invoke-BuildStep "dotnet restore" {
    dotnet restore
}

Invoke-BuildStep "dotnet build (-c $Configuration)" {
    dotnet build -c $Configuration --no-restore
}

Invoke-BuildStep "dotnet test (bez integracji live API)" {
    dotnet test -c $Configuration --no-build --filter "Category!=Integration"
}

Invoke-BuildStep "dotnet publish (self-contained $Runtime)" {
    dotnet publish $Project `
        -c $Configuration `
        -r $Runtime `
        --self-contained true `
        -p:PublishSingleFile=false `
        -p:PublishTrimmed=false
}

if (-not (Test-Path $PublishDir)) {
    throw "Brak folderu publish: $PublishDir"
}

$exePath = Join-Path $PublishDir "ZgrzytDesktop.exe"
if (-not (Test-Path $exePath)) {
    throw "Brak ZgrzytDesktop.exe w publish: $exePath"
}

if (-not (Test-Path $WriteReadmeScript)) {
    throw "Brak skryptu write-publish-readme.ps1: $WriteReadmeScript"
}

Write-Step "Generowanie README_RELEASE.txt w publish"
& $WriteReadmeScript -DestinationPath (Join-Path $PublishDir "README_RELEASE.txt")
Write-StepOk "README_RELEASE.txt wygenerowany"

# ---------------------------------------------------------------------------
# TODO: Podpisywanie kodu (Authenticode)
# Wymaga certyfikatu code-signing w magazynie Windows (CurrentUser/My lub maszynowy).
# Przyklad (odkomentuj po skonfigurowaniu certyfikatu):
#
#   $signtool = "${env:ProgramFiles(x86)}\Windows Kits\10\bin\10.0.22621.0\x64\signtool.exe"
#   if (-not (Test-Path $signtool)) { throw "Brak signtool.exe — zainstaluj Windows SDK." }
#   & $signtool sign /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 /a $exePath
#   if ($LASTEXITCODE -ne 0) { throw "Podpisywanie EXE nie powiodlo sie." }
# ---------------------------------------------------------------------------

Write-Step "Przygotowanie katalogu release"
New-ReleaseDirectory

if (Test-Path $ZipPath) {
    Remove-Item $ZipPath -Force
}

if (Test-Path $ChecksumPath) {
    Remove-Item $ChecksumPath -Force
}

Write-Step "Tworzenie archiwum ZIP"
Compress-Archive -Path (Join-Path $PublishDir "*") -DestinationPath $ZipPath -CompressionLevel Optimal -Force
Write-StepOk $ZipPath

Write-Step "Generowanie checksumy SHA256"
Write-Sha256Checksum -FilePath $ZipPath -OutputPath $ChecksumPath
Write-StepOk $ChecksumPath

Write-Host ""
Write-Host "Release gotowy." -ForegroundColor Green
Write-Host "  EXE:       $exePath"
Write-Host "  Publish:   $PublishDir"
Write-Host "  ZIP:       $ZipPath"
Write-Host "  SHA256:    $ChecksumPath"
Write-Host ""
Write-Host "Katalog release/ i jego zawartosc nie sa commitowane (patrz .gitignore)." -ForegroundColor DarkGray
