# ZgrzytDesktop

Desktopowy klient systemu **ZGRZYT** (Zintegrowany System Zgłoszeń i Rejestru Zdarzeń Technicznych) dla personelu **IT** i **administratorów**.

**Stack:** C# / .NET 10 / Avalonia UI / MVVM. Aplikacja komunikuje się z backendem Laravel przez REST API (`Bearer`, Laravel Sanctum). Konto z rolą zwykłego użytkownika (`user`) **nie ma dostępu** do panelu — po logowaniu wyświetlany jest komunikat o braku uprawnień i następuje wylogowanie.

## Główne funkcje

| Obszar | Opis |
|--------|------|
| **Logowanie** | Ręczne logowanie i auto-login przy starcie |
| **Role** | IT, admin, user (desktop tylko dla IT/admin) |
| **Lista zgłoszeń** | Wszystkie, aktywne, nieprzypisane |
| **Filtrowanie** | Status, priorytet, wyszukiwanie, sortowanie, paginacja |
| **Szczegóły zgłoszenia** | Podgląd, edycja statusu/priorytetu, zamknięcie, usunięcie |
| **Wiadomości** | Odczyt i dodawanie w wątku zgłoszenia |
| **Przypisywanie** | Admin wybiera IT/admin; IT — „Przypisz do mnie” |
| **Panel administracyjny** | Zarządzanie użytkownikami (admin), tworzenie kont |
| **Statystyki** | Liczba zgłoszeń, statusy, priorytety, przypisania |
| **Ustawienia** | Język, auto-wylogowanie, odświeżenie sesji |
| **Lokalny audyt** | Historia działań w aplikacji (plik DPAPI) |
| **Cache / offline** | Podgląd wcześniej zapisanych zgłoszeń przy braku API |
| **i18n** | Polski / angielski |
| **Auto-wylogowanie** | Wylogowanie po bezczynności (konfigurowalny timeout) |

## Wymagania

| Obszar | Wymaganie |
|--------|-----------|
| System | Windows 10/11 (x64) |
| Development | [.NET SDK 10](https://dotnet.microsoft.com/download) zgodny z `global.json` |
| Runtime (release) | Paczka **self-contained** nie wymaga instalacji .NET Runtime |
| Konto | **IT** lub **admin** w systemie ZGRZYT |
| Sieć | Dostęp do API (produkcyjnego lub lokalnego dev) |

Przed `dotnet build` zamknij uruchomioną aplikację, aby uniknąć blokady plików w `bin\`.

## API

| Środowisko | URL |
|------------|-----|
| Produkcja | `https://zgrzyt-api.onrender.com/api/` |
| Development (lokalnie) | `http://127.0.0.1:9000/api/` |

### Podstawowe endpointy

| Metoda | Endpoint | Opis |
|--------|----------|------|
| POST | `/api/login` | Logowanie, token Bearer |
| GET | `/api/user` | Profil zalogowanego użytkownika |
| POST | `/api/logout` | Wylogowanie sesji |
| POST | `/api/refresh` | Odświeżenie tokena |
| GET | `/api/tickets` | Lista zgłoszeń (paginacja Laravel) |
| GET | `/api/active-tickets` | Aktywne zgłoszenia (staff) |
| GET | `/api/unassigned-tickets` | Nieprzypisane zgłoszenia (staff) |
| GET/POST | `/api/tickets/{id}/messages` | Wiadomości w zgłoszeniu |
| POST | `/api/register` | Tworzenie konta (admin/IT) |
| POST | `/api/request-account` | Prośba o konto (osobny flow) |

### Uwagi techniczne

- **`GET /api/tickets`:** runtime API zwraca paginację Laravel (`current_page`, `data`, `last_page`, …).
- **Wiadomości:** desktop obsługuje odczyt i dodawanie; **edycja/usuwanie pojedynczych wiadomości** wymaga dedykowanych endpointów backendowych — aplikacja ich nie symuluje.
- **Tworzenie kont vs prośba o konto:** `POST /api/register` służy panelowi Administracja → Nowe konto; `POST /api/request-account` to osobny flow.

## Uruchomienie developerskie

```powershell
dotnet restore
dotnet build ZgrzytDesktop.sln -c Release
dotnet test ZgrzytDesktop.sln -c Release --filter "Category!=Integration"
dotnet run --project .\ZgrzytDesktop\ZgrzytDesktop.csproj
```

## Uruchomienie gotowej aplikacji

Artefakty release (GitHub Actions → workflow **Release** → Artifacts):

| Artefakt | Opis |
|----------|------|
| `ZgrzytDesktopSetup.exe` | Instalator Inno Setup — instaluje aplikację w `%LocalAppData%\Programs\ZgrzytDesktop\` |
| `ZgrzytDesktopUninstall.exe` | Dedykowany deinstalator — uruchamia `unins000.exe` zainstalowanej aplikacji (rejestr Windows + fallback) |
| `ZgrzytDesktop-win-x64-release.zip` | Wersja **portable** (self-contained folder w archiwum) |
| `*.sha256` | Sumy kontrolne SHA-256 do weryfikacji integralności pobranych plików |

**Portable ZIP:** rozpakuj **cały** folder publish i uruchom `ZgrzytDesktop.exe`. Usunięcie wersji portable = skasowanie folderu ręcznie (oraz opcjonalnie `%AppData%\ZgrzytDesktop\`).

**Instalator / deinstalator:** odinstalowanie przez Ustawienia Windows, menu Start lub `ZgrzytDesktopUninstall.exe` usuwa folder instalacji i dane w `%AppData%\ZgrzytDesktop\`.

**SmartScreen:** brak podpisu Authenticode może powodować ostrzeżenie Windows przy pierwszym uruchomieniu — zweryfikuj plik przez `.sha256` przed instalacją.

Lokalny build release: `.\scripts\publish-release.ps1` → katalog `release/` (gitignored).

### Weryfikacja SHA256 (PowerShell)

```powershell
$filePath = ".\ZgrzytDesktopSetup.exe"
$checksumPath = ".\ZgrzytDesktopSetup.exe.sha256"

$computed = (Get-FileHash -Path $filePath -Algorithm SHA256).Hash.ToLowerInvariant()
$expected = (Get-Content -Path $checksumPath -Raw).Trim().Split([char[]]@(' ', "`t"), [StringSplitOptions]::RemoveEmptyEntries)[0].ToLowerInvariant()

if ($computed -eq $expected) { Write-Host "SHA256 OK" } else { throw "SHA256 mismatch" }
```

## Testy

### Domyślny zestaw (bez live API)

```powershell
dotnet test ZgrzytDesktop.sln -c Release --filter "Category!=Integration"
```

### Projekty testowe

| Projekt | Zakres |
|---------|--------|
| `ZgrzytDesktop.Tests` | Testy jednostkowe, mock API, logika ViewModeli, serwisy, bezpieczeństwo |
| `ZgrzytDesktop.Headless.Tests` | Testy headless UI (Avalonia) — layout, binding, widoki |

### Testy integracyjne (live API, opcjonalne)

Wymagają zmiennych środowiskowych:

| Zmienna | Przykład |
|---------|----------|
| `ZGRZYT_API_URL` | `https://zgrzyt-api.onrender.com/api/` |
| `ZGRZYT_LOGIN` | login konta IT/admin |
| `ZGRZYT_PASSWORD` | hasło (tylko lokalnie / GitHub Secrets) |

```powershell
dotnet test ZgrzytDesktop.sln -c Release --filter "Category=Integration"
```

**Bez ustawionych zmiennych** testy `Category=Integration` są **Skipped** (nie Failed). Wzorzec konfiguracji: [.env.example](.env.example). Ręczny smoke API: `.\scripts\smoke-live-api.ps1`.

Testy live są **read-only** (poza `POST /api/logout` w izolowanej sesji) — nie tworzą zgłoszeń ani kont na produkcji.

## Coverage

- Coverage jest zbierany w **CI** (`coverlet.collector`) i uploadowany jako artefakt **CoverageReports** (Cobertura XML).
- Wyniki TRX trafiają do artefaktu **TestResults**.
- Testy integracyjne (`Category=Integration`) są wykluczone z coverage w CI i lokalnym skrypcie.
- Coverage obejmuje głównie: logikę serwisów, ViewModeli, bezpieczeństwo, storage, parsery i helpery — nie cały warstw UI Avalonia.

Lokalnie:

```powershell
.\scripts\test-coverage.ps1
.\scripts\test-coverage.ps1 -TryHtmlReport   # opcjonalny HTML (ReportGenerator)
```

Wyniki lokalne: katalog `coverage/` (gitignored).

## CI/CD

### `.github/workflows/ci.yml`

Uruchamiany przy push i pull request:

- `dotnet restore` → `dotnet build` (Release)
- testy jednostkowe (`Category!=Integration`) + headless
- upload **TestResults** (TRX)
- upload **CoverageReports** (Cobertura XML)

### `.github/workflows/release.yml`

Ręczny workflow (`workflow_dispatch`):

- build + test (bez integracji live)
- publish self-contained `win-x64`
- portable **ZIP** + SHA256
- instalator Inno Setup (`ZgrzytDesktopSetup.exe`) + SHA256
- dedykowany deinstalator (`ZgrzytDesktopUninstall.exe`) + SHA256

**Artefakty release (6 plików):**

- `ZgrzytDesktopSetup.exe`
- `ZgrzytDesktopSetup.exe.sha256`
- `ZgrzytDesktopUninstall.exe`
- `ZgrzytDesktopUninstall.exe.sha256`
- `ZgrzytDesktop-win-x64-release.zip`
- `ZgrzytDesktop-win-x64-release.zip.sha256`

## Architektura

- **UI:** Avalonia (XAML, style w `Styles/`, widoki w `Views/` i `Views/DashboardParts/`)
- **MVVM:** ViewModels z `CommunityToolkit.Mvvm` (`ObservableObject`, `[RelayCommand]`)
- **Shell:** `MainWindowViewModel` — przełączanie login ↔ dashboard, sesja, auto-login
- **Dashboard:** `DashboardViewModel` — fasada nawigacji, toasty, kompozycja paneli
- **Panele:** osobne ViewModele (`TicketsPanelViewModel`, `TicketDetailsPanelViewModel`, `AdminPanelViewModel`, `SettingsPanelViewModel`, `StatisticsPanelViewModel`, `AuditPanelViewModel`, …)
- **Koordynacja API:** `DashboardApiCoordinator` — refresh, offline, timeout
- **Serwisy:** `IApiService`, `IAuthService`, `ITicketService`, `IUserAdminService`, `ISettingsService`, cache, audyt — DI w `App.axaml.cs`
- **DTO / modele:** mapowanie JSON ↔ modele domenowe
- **Storage lokalny:** `%AppData%\ZgrzytDesktop\` — token, cache, ustawienia, audyt (DPAPI)

## Struktura repozytorium

```text
ZgrzytDesktop/
├── ZgrzytDesktop/              # aplikacja Avalonia
├── ZgrzytDesktop.Tests/        # testy jednostkowe
├── ZgrzytDesktop.Headless.Tests/
├── ZgrzytDesktop.Uninstaller/  # wrapper deinstalatora (release artifact)
├── installer/                  # Inno Setup (ZgrzytDesktop.iss)
├── assets/branding/            # logo, ikony instalatora
├── scripts/                    # publish-release, test-coverage, smoke-live-api, …
├── .github/workflows/          # ci.yml, release.yml
├── README.md
├── SECURITY.md
├── .env.example
└── ZgrzytDesktop.sln
```

## Użyte wzorce projektowe

| Wzorzec | Zastosowanie |
|---------|--------------|
| **MVVM** | Separacja widoków (AXAML) i logiki (`ViewModels/`) |
| **Dependency Injection** | Rejestracja serwisów w `App.axaml.cs` |
| **Service Layer** | `Services/` — API, auth, tickets, admin, settings |
| **Facade / Coordinator** | `DashboardViewModel`, `DashboardApiCoordinator` |
| **Command** | `[RelayCommand]` — akcje UI |
| **Observer / Data Binding** | Avalonia bindings, `INotifyPropertyChanged` |
| **Adapter / Wrapper** | `ZgrzytDesktop.Uninstaller` — uruchamianie `unins000.exe` |
| **DTO / Mapper** | Modele JSON, helpery konwersji |

## Bezpieczeństwo

Skrót: token i cache chronione **DPAPI**, hasła nie są zapisywane lokalnie, dashboard tylko dla IT/admin, HTTPS dla produkcyjnego API, SHA256 dla artefaktów release.

Szczegóły: **[SECURITY.md](SECURITY.md)** — zgłaszanie podatności, dane lokalne, sekrety, release security, checklist przed oddaniem.

## Dane lokalne

Katalog: `%AppData%\ZgrzytDesktop\`

| Plik | Zawartość | Ochrona |
|------|-----------|---------|
| `token.txt` | Bearer JWT | DPAPI |
| `Cache/tickets-cache.json` | Cache zgłoszeń | DPAPI |
| `Cache/user-cache.json` | Profil użytkownika | DPAPI |
| `audit-log.json` | Lokalny audyt | DPAPI |
| `Settings/settings.json` | Preferencje UI (język, auto-wylogowanie, API URL) | Plaintext JSON (bez sekretów) |
