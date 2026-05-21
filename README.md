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
| `dotnet test` | **153** passed, **5** skipped (integracja), **158** łącznie |
| `ZgrzytDesktop.Tests` | **147** passed, **5** skipped |
| `ZgrzytDesktop.Headless.Tests` | **6** passed |
| `dotnet publish` win-x64 | OK |

Większość testów używa mockowanego `HttpMessageHandler` i fake serwisów — nie wymaga żywego API. **5** testów integracyjnych (`Category=Integration`) jest pomijanych bez zmiennych `ZGRZYT_API_URL`, `ZGRZYT_LOGIN`, `ZGRZYT_PASSWORD` — szczegóły: [INTEGRATION_TESTS.md](INTEGRATION_TESTS.md).

```powershell
# tylko integracja na żywo (opcjonalnie, po ustawieniu env)
dotnet test --filter "Category=Integration"
```

Pokrycie obejmuje m.in.: ViewModele (login, main window, dashboard), audyt lokalny, statystyki, offline/cache, polling, i18n (`AppStrings`), testy headless UI (Avalonia) oraz serwisy/helpery z mock HTTP.

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
│   ├── Constants/                    # AppSections, AppRoles, statusy, …
│   └── Services/
│       ├── Interfaces/               # IAuthService, ITicketService, ISettingsService, …
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
