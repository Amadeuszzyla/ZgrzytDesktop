# ZGRZYT Desktop — status i ograniczenia

Dokument opisuje zgodność aplikacji Avalonia z wymaganiami projektu oraz ograniczenia wynikające z API (bez zmian backendu). Ostatnia aktualizacja: po Fazie 9A (finalny audyt, dokumentacja testów i jakości kodu).

## Co działa

### Uwierzytelnianie i sesja

- Logowanie (`POST /api/login`), Bearer, `GET /api/user`, wylogowanie (`POST /api/logout`)
- Autologowanie z tokenu w `%AppData%` (DPAPI)
- Przy odpowiedzi **401**: jedna próba `POST /api/refresh` (jeśli skonfigurowane w `ApiService.TryRefreshSessionAsync`), ponowienie żądania; przy ponownym 401 — wylogowanie
- Ręczne odświeżenie sesji w ustawieniach (`POST /api/refresh`)

### Zgłoszenia i wiadomości

- Listy: `GET tickets`, `active-tickets`, `unassigned-tickets` (widoki kolejki wg roli)
- Filtry: status, priorytet, wyszukiwanie tekstowe, paginacja
- **Sortowanie**: dwa ComboBoxy (pole + kierunek) → query `sort_by` / `sort_direction` (np. `created_at`, `desc`) — sortowanie po stronie API, **nie** przez kliknięcie nagłówków DataGrid
- Szczegóły zgłoszenia, wiadomości (`GET`/`POST .../messages`, pole JSON `body`)
- Tworzenie zgłoszenia, auto-odświeżanie listy (timer)
- Edycja statusu/priorytetu, przypisanie do siebie, zamknięcie, usunięcie — wg roli **it** / **admin** (i zamknięcie własnego zgłoszenia dla **user**, jeśli API pozwoli)

### Kategorie i statusy

- Kategorie UI: Hardware, Software, Sieć — zapis w tytule `[Kategoria]` i linii opisu `Kategoria: ...` (pole `category` **nie** jest wysyłane w body API)
- Statusy w UI: Nowe / W toku / Rozwiązane → API: `nowe` / `w trakcie` / `zamknięte`

### Administracja i konta

- **Administracja** (role **admin**): `GET users`, `active-users`, `inactive-users`, `banned-users`; `POST ban`, `activate`, `unban` (odbanowanie z hasłem w body)
- **Zgłoś nowe konto** (`POST /api/request-account`) — użytkownik w menu; it/admin w zakładce Administracja → Nowe konto

### Statystyki, audyt, UX

- Statystyki: domyślnie z **bieżącej strony** listy; przycisk agreguje wiele stron przez wielokrotne `GET tickets`
- **Lokalny audyt** (historia w szczegółach zgłoszenia i w ustawieniach) — zapis desktopowy, szyfrowany DPAPI
- Motyw: jasny / ciemny / system (`settings.json`)
- Język: **pl** / **en** (`AppStrings.resx` + `AppStrings.en.resx`, `UiCulture` w ustawieniach)
- Toasty w oknie aplikacji; tryb offline z cache zgłoszeń

### Jakość kodu i testy

- `DashboardViewModel` podzielony na **19** plików partial (Navigation, Tickets, TicketDetails, Admin, Settings, Statistics, Audit, Toast, Support, Localization, …)
- Główny plik `DashboardViewModel.cs`: ok. **289** linii fizycznie, ok. **126** niepustych; **partiale łącznie:** ok. **2427** linii
- Wspólna obsługa błędów API (`HandleApiError`, `ExecuteApiAsync`) — używana w mutacjach admin/ustawienia/część szczegółów; listy/statystyki/offline nadal z dedykowanym `catch`
- Stałe: `AppSections`, `AppRoles`, `TicketStatuses`, `TicketPriorities`, `FilterLabels`, `AdminTabs`, `ToastTypes`
- **Interfejsy serwisów** (`Services/Interfaces/`): `IAuthService`, `ITicketService`, `ISettingsService`, `ILocalAuditLogService`, `ITokenStorage`, `IUserAdminService` — ViewModele i testy zależą od abstrakcji (DIP)

**Weryfikacja build/test/publish:**

| Krok | Wynik |
|------|--------|
| `dotnet build` | OK — 0 błędów, 0 ostrzeżeń |
| `dotnet test` | **153** passed, **5** skipped (integracja), **158** łącznie |
| `ZgrzytDesktop.Tests` | **147** passed, **5** skipped |
| `ZgrzytDesktop.Headless.Tests` | **6** passed |
| `dotnet publish` `-c Release -r win-x64 --self-contained false` | OK |

**Projekty testowe:**

| Obszar | Projekt / pliki |
|--------|-----------------|
| ViewModele | `LoginViewModelTests`, `MainWindowViewModelTests`, `DashboardViewModelTests`, `DashboardStatisticsTests`, `DashboardPollingTests`, `DashboardOfflineCacheTests` |
| Audyt | `LocalAuditLogServiceTests` |
| i18n | `AppStringsTests` |
| Serwisy, helpery, modele | m.in. `AuthServiceTests`, `TicketServiceTests`, `UserAdminServiceTests`, `ApiErrorSanitizerTests`, `StatusDisplayHelperTests`, … |
| UI headless | `ZgrzytDesktop.Headless.Tests` — `HeadlessViewsTests` (6) |
| Integracja API (opcjonalna) | `LiveApiIntegrationTests` — skip bez env; [INTEGRATION_TESTS.md](INTEGRATION_TESTS.md) |

Testy jednostkowe i headless używają mock HTTP / fake serwisów — **nie** wymagają żywego API domyślnie.

## Ograniczenia API i produktu (świadome)

| Obszar | Ograniczenie |
|--------|----------------|
| `GET /api/logs` | Brak w OpenAPI — UI **nie** pobiera logów systemowych z backendu |
| Pole `category` | Nie w requestach tworzenia/edycji; tylko prefix/opis (unikanie 422) |
| Zamknięcie przez **user** | Przycisk „Zamknij” dla autora; przy **403** z API — komunikat w UI (backend może wymagać it/admin) |
| Czas pierwszej reakcji IT | Model `Ticket` ma `created_at`, `updated_at`, `closed_at` — **brak** `first_response_at` / SLA w API → desktop **nie** wyświetza prawdziwego czasu pierwszej odpowiedzi IT |
| Statystyki globalne | Bez dedykowanego raportu API — agregacja przez wielokrotne listowanie `tickets` |
| `DELETE /api/tickets/{id}` | W UI dla uprawnionych ról; przy braku uprawnień: **403** |
| Adres API | Stały w kodzie / `settings.json`; **brak** edycji URL w panelu ustawień |
| Powiadomienia Windows | **Brak** — nie używamy tray ani toastów systemowych (informacja tylko w aplikacji) |
| Panel admin | Przy braku roli admin: **403** z API |

## Pliki lokalne (AppData)

| Plik / folder | Zawartość |
|---------------|-----------|
| `token.txt` | Token dostępu (DPAPI) |
| `Cache/` | Cache użytkownika i zgłoszeń (DPAPI) |
| `audit-log.json` | Lokalny audyt działań (DPAPI) |
| `Settings/settings.json` | `ThemeMode`, `UiCulture`; `ApiBaseUrl` zapisany, ale **nie edytowany w UI** |

## Role w UI

| Rola | Uprawnienia w aplikacji |
|------|-------------------------|
| **user** | Lista, szczegóły, wiadomości, nowe zgłoszenia, zamknięcie własnego (jeśli API pozwoli), **Zgłoś nowe konto** |
| **it** | Jak wyżej + edycja statusu/priorytetu, przypisanie, zamknięcie/usuwanie wg API, Administracja → Nowe konto |
| **admin** | Jak it + zakładka **Użytkownicy** (filtry list, ban, aktywacja, odbanowanie) |

Autoryzacja końcowa zawsze po stronie API (401/403).

## Dane testowe

Tytuły i treści zgłoszeń na liście pochodzą z API — desktop nie filtruje treści z bazy. Przykładowe dane w środowisku dev czyści się po stronie backendu/bazy.

## Powiązane dokumenty

- [README.md](README.md) — uruchomienie, publish, skrót funkcji, podsumowanie testów
- [REQUIREMENTS.md](REQUIREMENTS.md) — wymagania środowiska
- [INTEGRATION_TESTS.md](INTEGRATION_TESTS.md) — testy integracyjne i ręczne wywołania API
