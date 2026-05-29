# Code coverage — ZGRZYT Desktop

Projekt testowy `ZgrzytDesktop.Tests` (i `ZgrzytDesktop.Headless.Tests`) używa pakietu **coverlet.collector** — coverage uruchamia się przez `dotnet test` bez globalnych narzędzi.

## Szybki start

```powershell
.\scripts\test-coverage.ps1
```

Z opcjonalnym raportem HTML (wymaga [ReportGenerator](#reportgenerator-opcjonalnie)):

```powershell
.\scripts\test-coverage.ps1 -TryHtmlReport
```

Wyniki trafiają do `coverage/` (katalog jest w `.gitignore`).

## Ręczna komenda

```powershell
dotnet test ZgrzytDesktop.sln `
  -c Release `
  --filter "Category!=Integration" `
  --collect:"XPlat Code Coverage" `
  --results-directory coverage/raw `
  -- `
  DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura
```

Pliki `coverage.cobertura.xml` pojawią się w podkatalogach `coverage/raw/<guid>/`.

## Co jest mierzone

| Projekt testowy | Coverage |
|-----------------|----------|
| `ZgrzytDesktop.Tests` | tak (`coverlet.collector`) |
| `ZgrzytDesktop.Headless.Tests` | tak (`coverlet.collector`) |

Testy `Category=Integration` (live API) są **pomijane** — nie wymagają `ZGRZYT_*` env i nie wpływają na wynik coverage w CI lokalnym.

## ReportGenerator (opcjonalnie)

Coverlet generuje XML (Cobertura). Czytelny raport HTML wymaga **ReportGenerator** — opcjonalnego narzędzia dotnet:

```powershell
dotnet tool install -g dotnet-reportgenerator-globaltool
.\scripts\test-coverage.ps1 -TryHtmlReport
```

Ręcznie:

```powershell
reportgenerator `
  "-reports:coverage/raw/**/coverage.cobertura.xml" `
  "-targetdir:coverage/report" `
  "-reporttypes:HtmlSummary;HtmlInline_AzurePipelines"
```

Otwórz `coverage/report/index.html` w przeglądarce.

Instalacja globalna nie jest wymagana do samego zebrania coverage — wystarczy `dotnet test` ze skryptu.

## CI

Przykład (GitHub Actions):

```yaml
- run: dotnet test ZgrzytDesktop.sln -c Release --filter "Category!=Integration" --collect:"XPlat Code Coverage" --results-directory coverage/raw
```

Artefakt: `coverage/raw/**/coverage.cobertura.xml`.

## Powiązane dokumenty

- [README.md](README.md) — sekcja Testy
- [INTEGRATION_TESTS.md](INTEGRATION_TESTS.md) — testy live API (pomijane w coverage script)
