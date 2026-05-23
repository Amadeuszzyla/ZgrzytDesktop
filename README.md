# ZGRZYT Desktop

Desktopowy klient systemu **ZGRZYT** (Zintegrowany System Zgłoszeń i Rejestru Zdarzeń Technicznych) dla personelu **IT** i **administratorów**.

Aplikacja (Avalonia, .NET 10, MVVM) komunikuje się z backendem Laravel przez REST API (`Bearer`, Laravel Sanctum). Konto z rolą zwykłego użytkownika (`user`) **nie ma dostępu** do panelu desktopowego — po logowaniu wyświetlany jest komunikat o braku uprawnień i następuje wylogowanie.

## Funkcje

| Obszar | Opis |
|--------|------|
| **Logowanie** | `POST /api/login`, token JWT w pamięci i w pliku chronionym DPAPI, `GET /api/user`, wylogowanie; przy **401** jedna próba `POST /api/refresh` |
| **Zgłoszenia** | Listy: wszystkie / aktywne / nieprzypisane (`tickets`, `active-tickets`, `unassigned-tickets`); filtry statusu i priorytetu, wyszukiwanie, sortowanie, paginacja |
| **Szczegóły** | Podgląd zgłoszenia, wątek wiadomości (`GET`/`POST .../messages`), edycja statusu i priorytetu, przypisanie do siebie, zamknięcie, usunięcie (wg uprawnień API) |
| **Administracja** | (**admin**) listy użytkowników, ban, aktywacja, odbanowanie; (**it** i **admin**) zakładka **Nowe konto** → `POST /api/register` z wyborem roli `user` / `it` / `admin` |
| **Statystyki** | KPI i wykresy z bieżącej strony lub agregacja wielu stron listy |
| **Lokalny audyt** | Historia działań w aplikacji (ustawienia + szczegóły zgłoszenia), plik szyfrowany DPAPI — **bez** `GET /api/logs` z backendu |
| **i18n** | Polski / angielski (`AppStrings`) |
| **Motyw** | Wyłącznie **jasny** (light-only) |
| **Offline** | Cache zgłoszeń przy niedostępności API |
| **Bezpieczeństwo lokalne** | Token, cache, audyt i ustawienia w `%AppData%\ZgrzytDesktop\` — szyfrowanie **DPAPI** (Windows) |

## API

Domyślny adres produkcyjny (kod / `settings.json`, bez edycji w UI):

```text
https://zgrzyt-api.onrender.com/api/
```

Lokalny development (opcjonalnie): `http://127.0.0.1:9000/api/`

### Tworzenie kont vs prośba o konto

| Endpoint | Zastosowanie |
|----------|----------------|
| `POST /api/register` | **Admin/IT** tworzą konto w **Administracja → Nowe konto** (body: `name`, `login`, `email`, `password`, `password_confirmation`, `role`) |
| `POST /api/request-account` | Osobny flow prośby o konto (`AuthService`) — **nie** służy do tworzenia kont przez panel administracyjny |

### Uwagi techniczne

- **`GET /api/tickets`:** OpenAPI czasem opisuje tablicę; runtime API zwraca **paginację Laravel** (`current_page`, `data`, `last_page`, …) — desktop jest zgodny z API produkcyjnym.
- **`first_response_at`:** opcjonalne w modelu; gdy API go nie zwraca, statystyki czasu reakcji pokazują **N/A** (brak fałszywego SLA).
- **Kolejki active/unassigned:** do API idą `page`, `per_page`, `search`; przy filtrach status/priorytet/sort — pobranie wielu stron i przetwarzanie lokalne (`TicketQueueListProcessor`).

## Wymagania

- Windows 10/11
- [.NET SDK 10](https://dotnet.microsoft.com/download) do budowy ze źródeł
- Konto **IT** lub **admin** w systemie ZGRZYT

Szczegóły środowiska: [REQUIREMENTS.md](REQUIREMENTS.md).

## Uruchomienie (developerskie)

```powershell
cd C:\ścieżka\do\ZgrzytDesktop
dotnet restore
dotnet build
dotnet test
dotnet run --project .\ZgrzytDesktop\ZgrzytDesktop.csproj
```

Przed `dotnet build` zamknij uruchomioną aplikację, aby uniknąć blokady plików w `bin\`.

## Testy

```powershell
dotnet test -c Release
```

| Projekt | Typowe wyniki |
|---------|----------------|
| `ZgrzytDesktop.Tests` | testy jednostkowe i integracyjne (integracja live **skipped** bez env) |
| `ZgrzytDesktop.Headless.Tests` | testy UI headless (Avalonia) |

Testy integracyjne na żywym API (opcjonalnie): zmienne `ZGRZYT_API_URL`, `ZGRZYT_LOGIN`, `ZGRZYT_PASSWORD` — patrz [INTEGRATION_TESTS.md](INTEGRATION_TESTS.md).

```powershell
dotnet test -c Release --filter "Category=Integration"
```

## Publikacja (Windows x64)

**Rekomendacja:** cały folder `publish` w archiwum ZIP (self-contained), nie sam plik EXE.

```powershell
.\scripts\publish-release.ps1
```

Skrypt: `clean` → `restore` → `build` → `test` (bez integracji live) → `publish` → kopiuje `README_RELEASE.txt` → tworzy `ZgrzytDesktop-win-x64-release.zip`.

Instrukcja dla użytkownika końcowego: [README_RELEASE.txt](README_RELEASE.txt).

## Architektura (skrót)

- **Shell:** `MainWindowViewModel` (login ↔ dashboard), `DashboardViewModel` (nawigacja, toasty, fasada etykiet).
- **Panele:** `TicketsPanelViewModel`, `TicketDetailsPanelViewModel`, `AdminPanelViewModel`, `SettingsPanelViewModel`, `StatisticsPanelViewModel`, `AuditPanelViewModel`.
- **Serwisy:** `IApiService`, `IAuthService`, `ITicketService`, `IUserAdminService`, `ISettingsService`, `ILocalAuditLogService`, cache — rejestracja w `App.axaml.cs`.
- **Widoki:** `Views/DashboardParts/` (topbar, listy kart zgłoszeń i użytkowników, formularze).

## Dane lokalne

Katalog: `%AppData%\ZgrzytDesktop\`

| Plik | Zawartość |
|------|-----------|
| `token.txt` | Bearer JWT (DPAPI) |
| `Cache/tickets-cache.json` | Cache zgłoszeń |
| `Cache/user-cache.json` | Profil użytkownika |
| `audit-log.json` | Lokalny audyt |
| `Settings/settings.json` | Język UI, adres API (bez haseł) |

## Struktura repozytorium

```text
ZgrzytDesktop/
├── ZgrzytDesktop/           # aplikacja Avalonia
├── ZgrzytDesktop.Tests/
├── ZgrzytDesktop.Headless.Tests/
├── scripts/publish-release.ps1
├── INTEGRATION_TESTS.md
├── REQUIREMENTS.md
├── README_RELEASE.txt
├── .env.example
└── ZgrzytDesktop.sln
```

## Powiązane dokumenty

- [REQUIREMENTS.md](REQUIREMENTS.md) — wymagania środowiska i produktu
- [INTEGRATION_TESTS.md](INTEGRATION_TESTS.md) — testy na żywym API
- [README_RELEASE.txt](README_RELEASE.txt) — instrukcja z paczki ZIP
