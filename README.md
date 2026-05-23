# ZGRZYT Desktop

Desktopowy klient systemu ZGRZYT — Zintegrowanego Systemu Zgłoszeń i Rejestru Zdarzeń Technicznych.

Aplikacja (Avalonia, MVVM) komunikuje się z backendem Laravel przez REST API (Bearer / Laravel Sanctum). Szczegóły zgodności z API, role i ograniczenia: [README_DESKTOP_STATUS.md](README_DESKTOP_STATUS.md).

## Wymagania

- Windows 10/11
- [.NET SDK 10](https://dotnet.microsoft.com/download) (projekt: `net10.0-windows`)
- Działające API ZGRZYT (domyślnie `https://zgrzyt-api.onrender.com/api/` — adres w kodzie / `settings.json`, **bez edycji w UI**)

## Uruchomienie

```powershell
cd C:\ścieżka\do\ZgrzytDesktop
dotnet restore
dotnet build
dotnet test
dotnet run --project .\ZgrzytDesktop\ZgrzytDesktop.csproj
```

**Uwaga:** przed `dotnet build` zamknij uruchomioną aplikację — inaczej pliki w `bin\` mogą być zablokowane.

### Cursor / VS Code

W repozytorium jest folder `.vscode/`:

- **Terminal → Run Task → `build`** — kompilacja
- **Run and Debug → `ZgrzytDesktop`** — uruchomienie (profil `coreclr`)

W Riderze: projekt startowy `ZgrzytDesktop`, profil uruchomienia.

## Testy

```powershell
dotnet test -c Release
```

**Stan weryfikacji (po Fazach 14–16, Release):**

| Projekt | Powodzenie | Pominięte | Łącznie |
|---------|------------|-----------|---------|
| `ZgrzytDesktop.Tests` | **289** | **5** (live API) | 294 |
| `ZgrzytDesktop.Headless.Tests` | **22** | 0 | 22 |
| **Razem** | **311** | **5** | **316** |

| Krok | Wynik |
|------|--------|
| `dotnet build -c Release` | OK — 0 błędów, 0 ostrzeżeń (typowo) |
| `dotnet test -c Release` | jak w tabeli powyżej |

Większość testów używa mockowanego `HttpMessageHandler` i fake serwisów — nie wymaga żywego API. **5** testów integracyjnych (`Category=Integration`) jest **pomijanych** bez zmiennych `ZGRZYT_API_URL`, `ZGRZYT_LOGIN`, `ZGRZYT_PASSWORD` — szczegóły: [INTEGRATION_TESTS.md](INTEGRATION_TESTS.md).

```powershell
# tylko integracja na żywo (opcjonalnie, po ustawieniu env)
dotnet test --filter "Category=Integration"
```

Pokrycie obejmuje m.in.: panele dashboardu, i18n (`AppStrings` PL/EN), kolejki zgłoszeń i filtry (Faza 16I), administracja użytkowników, audyt lokalny, offline/cache, polling, testy regresji Faz 16, testy headless UI (Avalonia: `ViewLocator`, `MainWindow`, layout dashboardu).

## Architektura (Fazy 14–16)

### Shell i nawigacja

- **Composition root:** `App.axaml.cs` — DI (`ConfigureServices`, `BuildServiceProvider`), `ServiceProvider.Dispose()` przy zamknięciu aplikacji.
- **`MainWindowViewModel`** — `CurrentViewModel` (login / dashboard), autologowanie, wylogowanie; `MainWindow` + `ViewLocator` (`ContentControl`, bez ręcznego podmiany widoków w `App`).
- **`DashboardViewModel`** — **shell / orchestrator**: nawigacja sekcji, toasty, etykiety UI, delegacja do panelowych ViewModeli; własne partiale (`Navigation`, `Panels`, `Localization`, `Toast`, …).

### Panele (osobne ViewModele)

| ViewModel | Odpowiedzialność |
|-----------|------------------|
| `TicketsPanelViewModel` | Lista zgłoszeń, filtry, sort, paginacja, tworzenie zgłoszenia (partiale: `List`, `Filters`, `Pagination`, `Create`) |
| `TicketDetailsPanelViewModel` | Szczegóły, wiadomości, edycja statusu/priorytetu, przypisanie, zamknięcie, usunięcie (partiale: `Load`, `Messages`, `Mutations`, `Permissions`) |
| `AdminPanelViewModel` | Listy użytkowników (filtry), ban / aktywacja / odbanowanie, nowe konto (staff) |
| `SettingsPanelViewModel` | Język UI, zapis ustawień, odświeżenie sesji |
| `StatisticsPanelViewModel` | KPI i wykresy (bieżąca strona / agregacja wielostronicowa) |
| `AuditPanelViewModel` | Lokalna historia audytu (ustawienia) |

### UI (layout)

- **`DashboardView.axaml`** — **topbar** (`DashboardTopBarView`: logo ZGRZYT, nawigacja pozioma, wylogowanie) + obszar treści (jedna sekcja na raz).
- **Zgłoszenia:** lista jako **karty** (`ticket-card`), nie DataGrid.
- **Administracja → użytkownicy:** lista jako **karty** (`admin-user-card`), nie DataGrid.
- **Motyw:** wyłącznie **jasny** (light-only); brak przełącznika dark/system w UI (`RequestedThemeVariant="Light"`).
- Układ i kolorystyka zbliżone do referencyjnego panelu webowego ZGRZYT.

Widoki: `Views/DashboardParts/` (`DashboardTopBarView`, `TicketsPanelView`, `TicketDetailsPanelView`, `StatisticsPanelView`, `AdminPanelView`, `SettingsPanelView`, `RequestAccountPanelView`). Bindingi w XAML wskazują na właściwości `DashboardViewModel` (fasada) lub bezpośrednio na panele tam, gdzie to uproszczono.

### i18n i serwisy

- **`AppStrings`** (`AppStrings.resx` + `AppStrings.en.resx`) — runtime PL/EN (`UiCulture` w ustawieniach).
- **Interfejsy:** `IApiService`, `IAuthService`, `ITicketService`, `ISettingsService`, `IUserAdminService`, cache, audyt — implementacje w `Services/`.
- **Cache / DPAPI:** `ILocalTicketCacheService`, `ILocalUserCacheService`, `LocalAuditLogService`.

## Publikacja i oddanie (Windows x64)

**Rekomendowany sposób przekazania programu:** cały folder `publish` spakowany do **ZIP** (self-contained). **Nie** wysyłaj samego pliku EXE — aplikacja wymaga wszystkich DLL i plików runtime w tym samym katalogu.

```powershell
.\scripts\publish-release.ps1
```

Skrypt wykonuje: `clean` → `restore` → `build` → `test` (bez `Category=Integration`) → `publish` (self-contained) → kopiuje `README_RELEASE.txt` → tworzy `ZgrzytDesktop-win-x64-release.zip`.

Ręcznie (ten sam wariant co skrypt):

```powershell
dotnet publish .\ZgrzytDesktop\ZgrzytDesktop.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=false
```

Artefakty:

```text
ZgrzytDesktop\bin\Release\net10.0-windows\win-x64\publish\ZgrzytDesktop.exe
ZgrzytDesktop-win-x64-release.zip   (w katalogu głównym repo — ignorowany przez git)
```

Instrukcja dla użytkownika końcowego: [README_RELEASE.txt](README_RELEASE.txt) (kopiowana do folderu publish przez skrypt).

**Self-contained** — na docelowym PC **nie** trzeba instalować .NET Desktop Runtime.

### Instalator (opcjonalnie, po oddaniu)

| Sposób | Kiedy |
|--------|--------|
| **ZIP folderu publish** | **Rekomendowane** — `scripts/publish-release.ps1` |
| **Inno Setup** | Opcjonalnie po oddaniu |
| **MSI / WiX / MSIX** | Opcjonalne, nie wymagane na tym etapie |

## Funkcje (zgodność ze specyfikacją)

| Obszar | Opis |
|--------|------|
| **Logowanie** | `POST /api/login`, Bearer (Sanctum), autologowanie z tokenu DPAPI, `GET /api/user`, wylogowanie; przy **401** jedna próba `POST /api/refresh` i ponowienie żądania |
| **Prośba o konto** | `POST /api/request-account` — menu użytkownika lub Administracja (it/admin) |
| **Zgłoszenia** | Kolejki: `tickets` / `active-tickets` / `unassigned-tickets` (staff); filtry status i priorytet, wyszukiwanie, sort (pole + kierunek), paginacja, auto-odświeżanie |
| **Filtry kolejek** | **Wszystkie:** filtry i sort w query API. **Aktywne / Nieprzypisane:** `search` w API; przy filtrze status/priorytet lub niestandardowym sorcie — pobranie kolejki i **filtrowanie/sort/paginacja po stronie klienta** (Faza 16I) |
| **Szczegóły i wiadomości** | Szczegóły zgłoszenia, wątek (`GET`/`POST .../messages`, pole `body`), edycja statusu/priorytetu, przypisanie, zamknięcie, usunięcie (wg roli) |
| **Statusy / priorytety** | API: `nowe` / `w trakcie` / `zamknięte`; UI PL: Nowe / W toku / **Zamknięte** (EN: Closed) |
| **Kategorie** | Hardware, Software, Sieć — prefiks `[Kategoria]` w tytule i `Kategoria: ...` w opisie (brak pola `category` w API) |
| **Role** | `user` / `it` / `admin` — widoczność menu i akcji |
| **Administracja** | (admin) listy: wszyscy / aktywni / nieaktywni / zbanowani; ban, aktywacja, odbanowanie (`POST .../unban` z hasłem); fallback lokalny przy **404** na wyspecjalizowanych endpointach |
| **Statystyki** | KPI z bieżącej strony; opcjonalna agregacja ze wszystkich stron `GET tickets` |
| **Lokalny audyt** | Historia działań w aplikacji (szczegóły + ustawienia), szyfrowany plik w AppData — **nie** logi backendu (`GET /api/logs` niedostępne) |
| **Toasty** | Komunikaty w oknie aplikacji (bez powiadomień systemowych Windows) |
| **i18n** | Polski / angielski — `AppStrings`, przełącznik w ustawieniach |
| **Motyw** | **Tylko jasny** (light-only); zapis w `settings.json` normalizowany do `Light` |
| **Offline** | Cache zgłoszeń przy niedostępności API |

## Znane ograniczenia

- Brak **`GET /api/logs`** — tylko lokalny audyt desktopowy.
- **Kategoria** — brak pola w API (zapis w tytule/opisie).
- **Zamknięcie przez usera** — przycisk dla autora; przy **403** komunikat z API.
- **Czas pierwszej reakcji IT** — brak `first_response_at` w modelu API.
- Adres API **nie jest edytowany** w ustawieniach (język i zapis ustawień tak).
- Brak **powiadomień Windows** — tylko toasty w aplikacji.

Pełna tabela: [README_DESKTOP_STATUS.md](README_DESKTOP_STATUS.md).

## Struktura repozytorium

```text
ZgrzytDesktop/
├── ZgrzytDesktop/
│   ├── ViewModels/
│   │   ├── DashboardViewModel*.cs      # shell + partiale
│   │   └── DashboardModules/           # TicketsPanel, TicketDetailsPanel, Admin, …
│   ├── Views/
│   │   ├── DashboardView.axaml         # topbar + panele treści
│   │   └── DashboardParts/
│   ├── Resources/                      # AppStrings.resx, AppStrings.en.resx
│   ├── Helpers/                        # display helpers, TicketQueueListProcessor, …
│   ├── Constants/
│   ├── Cache/
│   └── Services/Interfaces/
├── ZgrzytDesktop.Tests/
├── ZgrzytDesktop.Headless.Tests/
├── scripts/publish-release.ps1
├── INTEGRATION_TESTS.md
├── README_DESKTOP_STATUS.md
├── README_RELEASE.txt
└── ZgrzytDesktop.sln
```

## Dane lokalne

Katalog: `%AppData%\ZgrzytDesktop\` — token, cache, audyt, ustawienia (DPAPI). Szczegóły: [README_DESKTOP_STATUS.md](README_DESKTOP_STATUS.md).
