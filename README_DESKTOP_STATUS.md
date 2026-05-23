# ZGRZYT Desktop — status i ograniczenia

Dokument opisuje zgodność aplikacji Avalonia z wymaganiami projektu oraz ograniczenia wynikające z API (bez zmian backendu). **Ostatnia aktualizacja: po Fazie 17F** (ochrona danych lokalnych DPAPI, brama ról IT/Admin, bezpieczeństwo API).

## Co działa

### Uwierzytelnianie i sesja

- Logowanie (`POST /api/login`), Bearer (Laravel Sanctum), `GET /api/user`, wylogowanie (`POST /api/logout`)
- **Dostęp do aplikacji desktopowej: tylko role `it` i `admin`** (rola `user` jest blokowana po logowaniu i autologinie)
- Autologowanie z tokenu w `%AppData%\ZgrzytDesktop\token.txt` (**DPAPI CurrentUser**, bez plaintext JWT)
- Przy odpowiedzi **401**: jedna próba `POST /api/refresh`, ponowienie żądania; przy ponownym 401 — wylogowanie lokalne (token usuwany, powrót do logowania)
- Ręczne odświeżenie sesji w ustawieniach
- Produkcja: **HTTPS**; `http://` dozwolone wyłącznie dla localhost / 127.0.0.1 (dev)

### Zgłoszenia i wiadomości

- Listy: `GET tickets`, `active-tickets`, `unassigned-tickets` (kolejki wg roli **it** / **admin**)
- **Wszystkie:** filtry status, priorytet, wyszukiwanie, sort (`sort_by` / `sort_direction`), paginacja — parametry w query API
- **Aktywne / Nieprzypisane:** wyszukiwanie w API; przy wybranym statusie, priorytecie lub sorcie innym niż domyślny — pobranie kolejki (stronicowane po stronie serwera, agregacja po stronie klienta) i **filtrowanie / sort / paginacja lokalna** (`TicketQueueListProcessor`, Faza 16I)
- UI listy: **karty zgłoszeń** (`ticket-card`), nie tabela DataGrid
- Szczegóły zgłoszenia, wiadomości (`GET`/`POST .../messages`, pole JSON `body`)
- Tworzenie zgłoszenia, auto-odświeżanie listy (timer ~45 s)
- Edycja statusu/priorytetu, przypisanie, zamknięcie, usunięcie — wg roli **it** / **admin** (i zamknięcie własnego dla **user**, jeśli API pozwoli)

### Kategorie i statusy

- Kategorie UI: Hardware, Software, Sieć — zapis w tytule `[Kategoria]` i linii `Kategoria: ...` (pole `category` **nie** w body API)
- Statusy: API `nowe` / `w trakcie` / `zamknięte` → UI PL **Nowe** / **W toku** / **Zamknięte** (EN: New / In progress / **Closed**)

### Administracja i konta

- **Administracja** (staff): nawigacja w topbarze; **admin** — zakładka Użytkownicy
- Listy: `users`, `active-users`, `inactive-users`, `banned-users`; przy **404** na wyspecjalizowanym endpoincie — fallback `GET users` + filtr lokalny
- Akcje: `POST ban`, `activate`, `unban` (hasło w body)
- UI listy użytkowników: **karty** (`admin-user-card`), nie DataGrid
- **Zgłoś nowe konto** (`POST /api/request-account`) — użytkownik w menu; it/admin w Administracji

### Statystyki, audyt, UX

- Statystyki: KPI z bieżącej strony listy; przycisk agreguje wiele stron przez `GET tickets`
- **Lokalny audyt** (historia w szczegółach zgłoszenia i w ustawieniach) — zapis desktopowy, DPAPI; wpisy historyczne nie są migrowane przy zmianie języka — nowe wpisy w aktualnym języku UI
- **Motyw:** wyłącznie **jasny** (light-only); brak aktywnego wyboru dark/system w UI
- **Język:** **pl** / **en** (`AppStrings`, `UiCulture` w ustawieniach)
- **Layout:** topbar z logo ZGRZYT (`/Assets/zgrzyt-logo.png`), nawigacja pozioma, sekcje pełnoekranowe — zgodnie z referencją webową
- Toasty w oknie aplikacji; tryb offline z cache zgłoszeń

### Architektura UI i kodu (Fazy 14–16)

| Warstwa | Opis |
|---------|------|
| `MainWindowViewModel` | Login ↔ dashboard, autologowanie |
| `DashboardViewModel` | Shell: nawigacja, toasty, fasada etykiet, koordynacja paneli |
| `TicketsPanelViewModel` | Lista, filtry, sort, paginacja, tworzenie (partiale) |
| `TicketDetailsPanelViewModel` | Szczegóły, wiadomości, mutacje (partiale) |
| `AdminPanelViewModel` | Użytkownicy, ban/activate/unban, nowe konto |
| `SettingsPanelViewModel` | Język, zapis, refresh sesji |
| `StatisticsPanelViewModel` | KPI, wykresy, agregacja |
| `AuditPanelViewModel` | Lokalny dziennik w ustawieniach |
| `DashboardView` + `DashboardParts/` | Topbar + UserControl na sekcję |

- Stałe: `AppSections`, `AppRoles`, `TicketStatuses`, `TicketPriorities`, `FilterLabels`, `AdminTabs`, `ToastTypes`
- **Interfejsy** (`Services/Interfaces/`): `IApiService`, `IAuthService`, `ITicketService`, `ISettingsService`, `ILocalAuditLogService`, `ITokenStorage`, `IUserAdminService`, cache
- **DI:** `App.axaml.cs`; testy produkcyjnego DI — `ViewModelTestFactory` + fakes (bez `ServiceProvider` w testach jednostkowych)

### Jakość — weryfikacja build/test

| Krok | Wynik |
|------|--------|
| `dotnet build -c Release` | OK — 0 błędów, 0 ostrzeżeń (typowo) |
| `ZgrzytDesktop.Tests` | **~340+** passed, **8** skipped (integracja live API) |
| `ZgrzytDesktop.Headless.Tests` | **22** passed |
| **Razem (passed)** | **311** |
| `scripts/publish-release.ps1` | ZIP self-contained `ZgrzytDesktop-win-x64-release.zip` |

**Projekty testowe (skrót):**

| Obszar | Pliki / projekty |
|--------|------------------|
| Panele i dashboard | `TicketsPanelViewModelTests`, `TicketDetailsPanelViewModelTests`, `DashboardViewModelTests`, polling, offline, statystyki |
| i18n / regresja F16 | `LocalizationRuntimeTests`, `Phase16RegressionTests`, `Phase16HStragglersTests`, `Phase16IQueueFilterTests` |
| Serwisy | `TicketServiceTests`, `UserAdminServiceTests`, `AuthServiceTests`, … |
| Headless UI | `HeadlessViewsTests`, `ViewLocatorShellTests` |
| Integracja (opcjonalna) | `LiveApiIntegrationTests` — [INTEGRATION_TESTS.md](INTEGRATION_TESTS.md) |
| Bezpieczeństwo (17F) | `Phase17FSecurityTests`, `TokenStorageTests`, `DesktopAccessHelperTests`, `MainWindowDesktopAccessTests` |

Testy domyślne **nie** wymagają żywego API.

## Ograniczenia API i produktu (świadome)

| Obszar | Ograniczenie |
|--------|----------------|
| `GET /api/logs` | Brak w OpenAPI — UI **nie** pobiera logów systemowych z backendu |
| Pole `category` | Nie w requestach tworzenia/edycji; tylko prefix/opis |
| Zamknięcie przez **user** | Przycisk dla autora; przy **403** — komunikat (backend może wymagać it/admin) |
| Czas pierwszej reakcji IT | Brak `first_response_at` w API |
| Statystyki globalne | Agregacja przez wielokrotne `GET tickets` |
| `active-tickets` / `unassigned-tickets` | Brak udokumentowanych parametrów `status`/`priority`/`sort` jak w `GET tickets` — desktop stosuje **lokalny fallback** przy filtrach (Faza 16I) |
| Adres API | Stały w kodzie / `settings.json`; **brak** edycji URL w ustawieniach |
| Motyw | Tylko jasny — wybór dark/system usunięty z UI (Faza 16E) |
| Powiadomienia Windows | **Brak** — tylko toasty w aplikacji |
| Panel admin | Przy braku roli: **403** |
| Live test `POST /api/logout` | Stary Bearer nie powinien dawać 200 na `GET /api/user`; akceptowane 401/403/500 (500 = znane zachowanie backendu) |

## Pliki lokalne i ochrona danych (Faza 17F)

Wszystkie dane użytkownika: **`%AppData%\ZgrzytDesktop`** (`Environment.SpecialFolder.ApplicationData`). Aplikacja **nie** zapisuje nic w folderze publish ani w bieżącym katalogu roboczym. **Nie wymaga** uprawnień administratora Windows.

| Plik / folder | Zawartość | Ochrona |
|---------------|-----------|---------|
| `token.txt` | Bearer JWT | **DPAPI** `CurrentUser` (+ migracja ze starego plaintext) |
| `Cache/tickets-cache.json` | Zgłoszenia offline | **DPAPI** (+ migracja z plaintext JSON) |
| `Cache/user-cache.json` | Profil użytkownika offline | **DPAPI** |
| `audit-log.json` | Lokalny audyt | **DPAPI** |
| `Settings/settings.json` | `ThemeMode`, `UiCulture`, `ApiBaseUrl` | Brak haseł/tokenów w modelu; tylko konfiguracja UI |

- Szyfrowanie: `System.Security.Cryptography.ProtectedData` (entropy aplikacji), **bez własnej kryptografii**.
- ACL katalogów: standardowe uprawnienia profilu użytkownika; zawartość i tak jest szyfrowana DPAPI dla tego konta.
- Logi: brak `Console`/`Debug` z tokenami; `SensitiveDataRedactor` do redakcji Bearer/JWT w tekstach.
- Błędy API: `ApiErrorSanitizer` usuwa HTML, stack trace i techniczne zrzuty.

## Role w UI

| Rola | Desktop |
|------|---------|
| **user** | **Brak dostępu** — komunikat `Login_DesktopAccessDenied`, wylogowanie, czyszczenie cache |
| **it** | Pełny dostęp staff (zgłoszenia, kolejki, administracja → nowe konto) |
| **admin** | Jak it + zakładka **Użytkownicy** |

Autoryzacja operacji po stronie API (401/403) pozostaje bez zmian.

## Dystrybucja

| Sposób | Status |
|--------|--------|
| **ZIP folderu publish** (self-contained) | **Rekomendowane** — `scripts/publish-release.ps1`, [README_RELEASE.txt](README_RELEASE.txt) |
| **Inno Setup / MSI / MSIX** | Opcjonalne po oddaniu |

Użytkownik końcowy: **rozpakować cały ZIP** i uruchomić `ZgrzytDesktop.exe` z folderu publish — **nie** przenosić samego EXE.

Artefakty `bin/`, `obj/`, `publish/`, `*.zip` — w `.gitignore`.

## Powiązane dokumenty

- [README.md](README.md) — uruchomienie, architektura, publish, testy
- [REQUIREMENTS.md](REQUIREMENTS.md) — wymagania środowiska
- [INTEGRATION_TESTS.md](INTEGRATION_TESTS.md) — testy integracyjne i ręczne API
