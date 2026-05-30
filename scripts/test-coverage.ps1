#Requires -Version 5.1
<#
.SYNOPSIS
    Uruchamia testy (bez integracji live API) i generuje raport code coverage.

.DESCRIPTION
    Wymaga tylko .NET SDK i pakietu coverlet.collector w projektach testowych.
    Wyniki trafiaja do ./coverage/ (gitignored).

.PARAMETER Configuration
    Konfiguracja buildu (domyslnie Release).

.PARAMETER TryHtmlReport
    Jesli dostepne narzedzie ReportGenerator (dotnet tool), generuje raport HTML.
#>
[CmdletBinding()]
param(
    [string] $Configuration = "Release",
    [switch] $TryHtmlReport
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $RepoRoot

$Solution = Join-Path $RepoRoot "ZgrzytDesktop.sln"
$CoverageRoot = Join-Path $RepoRoot "coverage"
$ResultsDir = Join-Path $CoverageRoot "raw"
$ReportDir = Join-Path $CoverageRoot "report"

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
        throw "Krok nie powiodl sie: $Label (kod wyjscia: $LASTEXITCODE)."
    }
}

function Get-CoberturaReports {
    Get-ChildItem -Path $ResultsDir -Recurse -Filter "coverage.cobertura.xml" -File -ErrorAction SilentlyContinue
}

function Try-InvokeReportGenerator {
    param([Parameter(Mandatory)][System.IO.FileInfo[]] $Reports)

    if ($Reports.Count -eq 0) {
        Write-Host "    Brak plikow coverage.cobertura.xml do raportu HTML." -ForegroundColor DarkYellow
        return $false
    }

    $reportPaths = ($Reports | ForEach-Object { $_.FullName }) -join ';'
    $reportGenerator = Get-Command reportgenerator -ErrorAction SilentlyContinue

    if (-not $reportGenerator) {
        Write-Host "    ReportGenerator niedostepny (opcjonalnie: dotnet tool install -g dotnet-reportgenerator-globaltool)." -ForegroundColor DarkGray
        return $false
    }

    if (Test-Path $ReportDir) {
        Remove-Item $ReportDir -Recurse -Force
    }

    New-Item -ItemType Directory -Path $ReportDir | Out-Null

    & reportgenerator `
        "-reports:$reportPaths" `
        "-targetdir:$ReportDir" `
        "-reporttypes:HtmlSummary;HtmlInline_AzurePipelines"

    if ($LASTEXITCODE -ne 0) {
        throw "ReportGenerator zakonczyl sie bledem (kod wyjscia: $LASTEXITCODE)."
    }

    return $true
}

Write-Host "ZGRZYT Desktop - test coverage ($Configuration)" -ForegroundColor White
Write-Host "Repozytorium: $RepoRoot" -ForegroundColor DarkGray

if (Test-Path $CoverageRoot) {
    Remove-Item $CoverageRoot -Recurse -Force
}

New-Item -ItemType Directory -Path $ResultsDir | Out-Null

Invoke-BuildStep "dotnet restore" {
    dotnet restore $Solution
}

Invoke-BuildStep "dotnet build (-c $Configuration)" {
    dotnet build $Solution -c $Configuration --no-restore
}

Invoke-BuildStep "dotnet test + coverlet (bez integracji live API)" {
    dotnet test $Solution `
        -c $Configuration `
        --no-build `
        --filter "Category!=Integration" `
        --collect:"XPlat Code Coverage" `
        --results-directory $ResultsDir `
        -- `
        DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura
}

$coberturaReports = @(Get-CoberturaReports)

if ($coberturaReports.Count -eq 0) {
    throw "Nie znaleziono plikow coverage.cobertura.xml w $ResultsDir."
}

Write-Step "Zebrane pliki Cobertura"
foreach ($report in $coberturaReports) {
    Write-Host "    $($report.FullName)" -ForegroundColor DarkGray
}
Write-StepOk "$($coberturaReports.Count) plik(ow)"

$htmlGenerated = $false
if ($TryHtmlReport) {
    Write-Step "Generowanie raportu HTML (ReportGenerator, opcjonalnie)"
    $htmlGenerated = Try-InvokeReportGenerator -Reports $coberturaReports
    if ($htmlGenerated) {
        Write-StepOk (Join-Path $ReportDir "index.html")
    }
}

Write-Host ""
Write-Host "Coverage gotowy." -ForegroundColor Green
Write-Host "  Raw:     $ResultsDir"
if ($htmlGenerated) {
    Write-Host "  HTML:    $ReportDir\index.html"
}
Write-Host ""
Write-Host "Szczegoly: README.md (sekcja Coverage)" -ForegroundColor DarkGray
