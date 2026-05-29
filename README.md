# ZGRZYT Desktop

Desktopowy klient systemu **ZGRZYT** (Zintegrowany System Zgłoszeń i Rejestru Zdarzeń Technicznych) dla personelu **IT** i **administratorów**.

Aplikacja (Avalonia, .NET 10, MVVM) komunikuje się z backendem Laravel przez REST API (`Bearer`, Laravel Sanctum). Konto z rolą zwykłego użytkownika (`user`) **nie ma dostępu** do panelu desktopowego — po logowaniu wyświetlany jest komunikat o braku uprawnień i następuje wylogowanie.

## Funkcje

| Obszar | Opis |
|--------|------|
| **Logowanie** | `POST /api/login`, token JWT w pamięci i w pliku chronionym DPAPI, `GET /api/user`, wylogowanie; przy **401** jedna próba `POST /api/refresh` |
| **Zgłoszenia** | Listy: wszystkie / aktywne / nieprzypisane (`tickets`, `active-tickets`, `unassigned-tickets`); filtry statusu i priorytetu, wyszukiwanie, sortowanie, paginacja |
| **Szczegóły** | Podgląd zgłoszenia, wątek wiadomości (`GET`/`POST .../messages` — odczyt i dodawanie), edycja statusu i priorytetu, przypisanie (`assigned_it_id`: admin wybiera IT/admin, IT — „Przypisz do mnie”), zamknięcie, usunięcie (wg uprawnień API) |
| **Administracja** | (**admin**) listy użytkowników, ban, aktywacja, odbanowanie; (**it** i **admin**) zakładka **Nowe konto** → `POST /api/register` z wyborem roli `user` / `it` / `admin` |
| **Statystyki** | Liczba zgłoszeń, statusy, priorytety, przypisania/nieprzypisane; zakres: bieżąca strona lub wszystkie strony listy. Czas pierwszej reakcji **nie** jest prezentowany — API nie dostarcza stabilnych danych do jego obliczenia. |
| **Lokalny audyt** | Historia działań w aplikacji (ustawienia + szczegóły zgłoszenia), plik szyfrowany DPAPI — **bez** `GET /api/logs` z backendu |
| **i18n** | Polski / angielski (`AppStrings`) |
| **Motyw** | Wyłącznie **jasny** (light-only) |
| **Offline** | Cache zgłoszeń przy niedostępności API |
| **Auto-wylogowanie** | `SessionInactivityMonitor` — wylogowanie po bezczynności; włącz/wyłącz i timeout (15 / 30 / 60 min) przez `AppSettings.AutoLogoutEnabled` i `AppSettings.AutoLogoutTimeoutMinutes` (`settings.json`) |
| **Bezpieczeństwo lokalne** | Token, cache i audyt w `%AppData%\ZgrzytDesktop\` — szyfrowanie **DPAPI** (Windows); ustawienia UI jako zwykły JSON (bez sekretów) |

## Zabezpieczenia po stronie aplikacji desktopowej

- **Uwierzytelnianie tokenowe** — logowanie przez `POST /api/login`, Bearer JWT w pliku chronionym DPAPI, odświeżanie sesji (`POST /api/refresh`), wylogowanie lokalne i przez API
- **Kontrola dostępu na podstawie roli** — dashboard tylko dla ról **IT** i **admin**; konto `user` po logowaniu otrzymuje komunikat i jest wylogowywane
- **Role-based UI w administracji** — admin: lista użytkowników, ban/aktywacja/odbanowanie; IT: tylko zakładka **Nowe konto**; niedostępne sekcje i akcje są ukryte, nie wyłączone
- **Szyfrowanie lokalnych danych (Windows DPAPI)** — token (`token.txt`), cache zgłoszeń, cache użytkownika i lokalny audyt w `%AppData%\ZgrzytDesktop\`
- **Ustawienia aplikacji (plaintext JSON)** — `Settings/settings.json` przechowuje wyłącznie preferencje UI (język, motyw, auto-wylogowanie, adres API); **bez** haseł i tokenów — celowo nie szyfrowane DPAPI
- **Oczyszczanie HTML przed wyświetleniem** — tytuł/opis zgłoszenia i treść wiadomości (`HtmlTextSanitizer`); odrzucanie odpowiedzi HTML z API jako błędów (`ApiErrorSanitizer`)
- **Brak logowania haseł i tokenów** — maskowanie wrażliwych pól w lokalnym audycie i komunikatach błędów API (`SensitiveDataMasker`, `SensitiveDataRedactor`)
- **Obsługa błędów bez stack trace** — przyjazne, zlokalizowane komunikaty (PL/EN); stack trace i HTML z odpowiedzi API nie trafiają do UI
- **Lokalizowana obsługa błędów logowania** — `LoginErrorMapper` mapuje wyjątki API na komunikaty `AppStrings`
- **Komunikacja z API przez HTTPS** — domyślny adres produkcyjny używa `https://`; zdalne `http://` jest automatycznie podnoszone do HTTPS (`ApiUrlSecurityHelper`); lokalny dev może używać `http://127.0.0.1`
- **Wylogowanie czyści sesję lokalną** — token, cache zgłoszeń, cache użytkownika; język UI pozostaje w ustawieniach
- **Brak fałszywej edycji/usuwania wiadomości** — desktop obsługuje tylko odczyt i dodawanie wiadomości; brak symulacji `PUT`/`DELETE` na wątku czatu

### Znane ograniczenia

- **Pełny audyt serwerowy** — wymaga endpointów backendowych (np. `GET /api/logs`); obecnie audyt jest **lokalny** (plik DPAPI)
- **Edycja/usuwanie wiadomości** — wymaga dedykowanych endpointów backendowych; desktop ich nie symuluje
- **Autoryzacja po stronie API** — UI ukrywa niedostępne akcje, ale ostateczna kontrola uprawnień musi być w backendzie
- **Rate limiting, rotacja tokenów po stronie serwera** — poza zakresem aplikacji desktopowej

**Nie da się wymusić wyłącznie z desktopu:** TLS/HTTPS po stronie serwera, timeout sesji API, autoryzacja endpointów, serwerowa walidacja danych.

## API

Domyślny adres produkcyjny (kod / `settings.json`, bez edycji URL w UI ustawień):

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
- **Kolejki active/unassigned:** do API idą `page`, `per_page`, `search`; przy filtrach status/priorytet/sort — pobranie wielu stron i przetwarzanie lokalne (`TicketQueueListProcessor`).
- **Wiadomości w czacie:** brak w desktopie edycji/usuwania pojedynczych wiadomości — API nie udostępnia stabilnych endpointów `PUT`/`PATCH`/`DELETE` na `.../messages/{id}`; aplikacja nie ukrywa ani nie fałszuje treści lokalnie.

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

### Code coverage

```powershell
.\scripts\test-coverage.ps1
```

Coverlet (`coverlet.collector` w projektach testowych), bez integracji live API. Szczegóły: [TEST_COVERAGE.md](TEST_COVERAGE.md).

## Publikacja (Windows x64)

**Rekomendacja:** cały folder `publish` w archiwum ZIP (self-contained), nie sam plik EXE.

```powershell
.\scripts\publish-release.ps1
```

Skrypt: `clean` → `restore` → `build` → `test` (bez integracji live) → `publish` → kopiuje `README_RELEASE.txt` → tworzy archiwum i checksumę:

| Artefakt | Ścieżka |
|----------|---------|
| ZIP | `release/ZgrzytDesktop-win-x64-release.zip` |
| SHA256 | `release/ZgrzytDesktop-win-x64-release.zip.sha256` |

Katalog `release/` jest w `.gitignore` i **nie jest commitowany**.

Instrukcja dla użytkownika końcowego: [README_RELEASE.txt](README_RELEASE.txt).

## Architektura (skrót)

- **Shell:** `MainWindowViewModel` (login ↔ dashboard), `DashboardViewModel` (nawigacja, toasty, fasada etykiet).
- **Panele:** `TicketsPanelViewModel`, `TicketDetailsPanelViewModel`, `AdminPanelViewModel`, `SettingsPanelViewModel`, `StatisticsPanelViewModel`, `AuditPanelViewModel`.
- **Serwisy:** `IApiService`, `IAuthService`, `ITicketService`, `IUserAdminService`, `ISettingsService`, `ILocalAuditLogService`, cache — rejestracja w `App.axaml.cs`.
- **Widoki:** `Views/DashboardParts/` (topbar, listy kart zgłoszeń i użytkowników, formularze).

## Dane lokalne

Katalog: `%AppData%\ZgrzytDesktop\`

| Plik | Zawartość | Ochrona |
|------|-----------|---------|
| `token.txt` | Bearer JWT | **DPAPI** |
| `Cache/tickets-cache.json` | Cache zgłoszeń (tytuły, opisy) | **DPAPI** |
| `Cache/user-cache.json` | Profil użytkownika (sesja) | **DPAPI** |
| `audit-log.json` | Lokalny audyt | **DPAPI** |
| `Settings/settings.json` | Język UI, motyw, auto-wylogowanie, adres API | Plaintext JSON (brak sekretów) |

## Struktura repozytorium

```text
ZgrzytDesktop/
├── ZgrzytDesktop/           # aplikacja Avalonia
├── ZgrzytDesktop.Tests/
├── ZgrzytDesktop.Headless.Tests/
├── scripts/publish-release.ps1
├── scripts/test-coverage.ps1
├── INTEGRATION_TESTS.md
├── TEST_COVERAGE.md
├── SECURITY.md
├── REQUIREMENTS.md
├── README_RELEASE.txt
├── .env.example
└── ZgrzytDesktop.sln
```

## Powiązane dokumenty

- [REQUIREMENTS.md](REQUIREMENTS.md) — wymagania środowiska i produktu
- [SECURITY.md](SECURITY.md) — zgłaszanie podatności, dane lokalne, sekrety, release
- [INTEGRATION_TESTS.md](INTEGRATION_TESTS.md) — testy na żywym API
- [TEST_COVERAGE.md](TEST_COVERAGE.md) — code coverage (coverlet)
- [README_RELEASE.txt](README_RELEASE.txt) — instrukcja z paczki ZIP
