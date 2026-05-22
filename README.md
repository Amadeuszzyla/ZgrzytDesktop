# ZGRZYT Desktop

Desktopowy klient systemu ZGRZYT — Zintegrowanego Systemu Zgłoszeń i Rejestru Zdarzeń Technicznych.

Aplikacja (Avalonia, MVVM) komunikuje się z backendem Laravel przez REST API. Szczegóły zgodności z API, role i ograniczenia: [README_DESKTOP_STATUS.md](README_DESKTOP_STATUS.md).

## Wymagania

- Windows 10/11
- [.NET SDK 10](https://dotnet.microsoft.com/download) (projekt: `net10.0-windows`)
- Działające API ZGRZYT (domyślnie `http://127.0.0.1:9000/api/` — adres ustawiany w kodzie / `settings.json`, **bez edycji w UI**)

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
dotnet test
```

**Stan weryfikacji (finalny audyt):**

| Krok | Wynik |
|------|--------|
| `dotnet build` | OK — 0 błędów, 0 ostrzeżeń |
| `dotnet test` | **158** passed, **5** skipped (integracja), **163** łącznie |
| `ZgrzytDesktop.Tests` | **147** passed, **5** skipped |
| `ZgrzytDesktop.Headless.Tests` | **11** passed |
| `dotnet publish` win-x64 | OK |

Większość testów używa mockowanego `HttpMessageHandler` i fake serwisów — nie wymaga żywego API. **5** testów integracyjnych (`Category=Integration`) jest pomijanych bez zmiennych `ZGRZYT_API_URL`, `ZGRZYT_LOGIN`, `ZGRZYT_PASSWORD` — szczegóły: [INTEGRATION_TESTS.md](INTEGRATION_TESTS.md).

```powershell
# tylko integracja na żywo (opcjonalnie, po ustawieniu env)
dotnet test --filter "Category=Integration"
```

Pokrycie obejmuje m.in.: ViewModele (login, main window, dashboard), audyt lokalny, statystyki, offline/cache, polling, i18n (`AppStrings`), testy headless UI (Avalonia, `ViewLocatorShellTests`: `ViewLocator.Build`, `MainWindow` + `ContentControl`) oraz serwisy/helpery z mock HTTP.

## Architektura (Fazy 9B–10)

- **Composition root:** `App.axaml.cs` — `Microsoft.Extensions.DependencyInjection` (`ConfigureServices`, `BuildServiceProvider`), `ServiceProvider.Dispose()` przy `desktop.Exit` / `ShutdownRequested` (jednorazowo, bez blokowania zamknięcia).
- **`IApiService`** — warstwa HTTP; `AuthService`, `TicketService`, `UserAdminService` zależą od interfejsu; `ApiService.TryRefreshSessionAsync` ustawiane w root po zbudowaniu providera.
- **Shell runtime:** `MainWindowViewModel` (`CurrentViewModel`, autologowanie, login/logout); `MainWindow.axaml` → `ContentControl` (`Content="{Binding CurrentViewModel}"`) + `ViewLocator` w `Application.DataTemplates`.
- **App nie przełącza widoków ręcznie** — brak `_mainWindow.Content = new LoginView/DashboardView`.
- **Dashboard UI:** `DashboardView.axaml` (layout) + `Views/DashboardParts/` (UserControl: sidebar, zgłoszenia, szczegóły, statystyki, ustawienia, admin, prośba o konto) — wspólny `DataContext` z `DashboardViewModel`.
- **Cache:** `ILocalTicketCacheService`, `ILocalUserCacheService` (`Cache/LocalTicketCacheService`, `LocalUserCacheService`); VM i test factory zależą od interfejsów.
- **Błędy API w VM:** `ExecuteApiAsync` w partialach (admin, szczegóły, ustawienia, prośba o konto, tworzenie zgłoszenia, statystyki wielostronicowe); listy/offline/auto-refresh/`LoadTicketDetails` — dedykowany `catch`.
- **Testy produkcyjnego DI:** nie używają `ServiceProvider` — `ViewModelTestFactory` + `MainWindowDependencies` (wewnętrzny helper testowy).

## Publikacja (Windows x64)

```powershell
dotnet publish .\ZgrzytDesktop\ZgrzytDesktop.csproj -c Release -r win-x64 --self-contained false
```

Artefakt:

```text
ZgrzytDesktop\bin\Release\net10.0-windows\win-x64\publish\ZgrzytDesktop.exe
```

Na docelowym PC wymagany jest zainstalowany **.NET 10 Desktop Runtime** (przy `--self-contained false`).

Ostatnia weryfikacja publish: **OK** (`Release`, `win-x64`, framework-dependent).

## Funkcje

| Obszar | Opis |
|--------|------|
| **Logowanie** | `POST /api/login`, Bearer, autologowanie z zapisanego tokenu, wylogowanie, przy **401** jedna próba `POST /api/refresh` i ponowienie żądania |
| **Zgłoszenia** | Listy `tickets` / `active-tickets` / `unassigned-tickets`, filtry status/priorytet, wyszukiwanie, **sortowanie API** (ComboBox: pole + kierunek → `sort_by` / `sort_direction`), paginacja, auto-odświeżanie listy |
| **Szczegóły i wiadomości** | Szczegóły zgłoszenia, wątek wiadomości (`GET`/`POST .../messages`), edycja statusu/priorytetu, przypisanie, zamknięcie, usunięcie (wg roli) |
| **Role** | `user` / `it` / `admin` — widoczność akcji i menu |
| **Administracja** | (admin) listy użytkowników: wszyscy / aktywni / nieaktywni / zbanowani; ban, aktywacja, odbanowanie z hasłem (`POST .../unban`) |
| **Nowe konto** | `POST /api/request-account` — menu użytkownika lub zakładka w Administracji (it/admin) |
| **Statystyki** | KPI z bieżącej strony listy; opcjonalna agregacja ze wszystkich stron (`GET tickets` w pętli) |
| **Lokalny audyt** | Historia działań w aplikacji (szczegóły zgłoszenia + ustawienia), szyfrowany plik w AppData — **nie** logi backendu |
| **Motyw** | Jasny / ciemny / zgodny z systemem — zapis w `settings.json` |
| **Język UI** | Polski / angielski (`AppStrings`, `UiCulture` w ustawieniach) |
| **Dane lokalne** | Token, cache użytkownika i zgłoszeń, audyt — szyfrowanie **DPAPI** (Windows) |
| **Offline** | Cache zgłoszeń i tryb offline przy braku API; toasty w aplikacji (bez powiadomień systemowych Windows) |

## Znane ograniczenia

- Brak endpointu **`GET /api/logs`** w OpenAPI — UI nie pobiera logów systemowych z backendu (tylko lokalny audyt desktopowy).
- **Kategoria** zgłoszenia: brak pola w API — zapis jako prefiks `[Kategoria]` w tytule i linia `Kategoria: ...` w opisie.
- **Zamknięcie przez usera**: przycisk widoczny dla autora; jeśli backend zwróci **403**, aplikacja pokazuje komunikat (autoryzacja po stronie API).
- **Czas pierwszej reakcji IT**: model `Ticket` nie zawiera pola typu `first_response_at` — statystyki i UI nie pokazują prawdziwego SLA pierwszej odpowiedzi, dopóki API go nie udostępni.
- Adres API **nie jest edytowany** w panelu ustawień (tylko motyw i język).
- Brak **powiadomień Windows** (tray/toast systemowy) — informacja zwrotna przez toasty w oknie aplikacji.

Pełna tabela: [README_DESKTOP_STATUS.md](README_DESKTOP_STATUS.md).

## Struktura repozytorium

```text
ZgrzytDesktop/
├── ZgrzytDesktop/                    # aplikacja Avalonia (MVVM, partial ViewModels)
│   ├── ViewModels/                   # DashboardViewModel.*.cs (Navigation, Tickets, Admin, …)
│   ├── Views/
│   │   ├── DashboardView.axaml       # shell dashboardu
│   │   └── DashboardParts/           # panele UserControl (bez osobnych VM)
│   ├── Constants/                    # AppSections, AppRoles, statusy, …
│   ├── Cache/                        # Local*CacheService + interfejsy w Services/Interfaces
│   └── Services/
│       ├── Interfaces/               # IApiService, IAuthService, ITicketService, cache, …
│       └── …                         # implementacje (ApiService, AuthService, …)
├── ZgrzytDesktop.Tests/              # testy jednostkowe i integracyjne (opcjonalne)
├── ZgrzytDesktop.Headless.Tests/     # testy UI headless (Avalonia)
├── INTEGRATION_TESTS.md              # ręczne i automatyczne testy API
├── .vscode/                          # launch.json, tasks.json
├── README_DESKTOP_STATUS.md
├── REQUIREMENTS.md
└── ZgrzytDesktop.sln
```

## Dane lokalne

Katalog: `%AppData%\ZgrzytDesktop\` — token, cache, audyt, ustawienia. Szczegóły i szyfrowanie: [README_DESKTOP_STATUS.md](README_DESKTOP_STATUS.md).
