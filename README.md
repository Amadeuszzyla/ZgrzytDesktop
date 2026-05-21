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

Testy jednostkowe (91) używają mockowanego `HttpMessageHandler` — nie wymagają żywego API.

## Publikacja (Windows x64)

```powershell
dotnet publish .\ZgrzytDesktop\ZgrzytDesktop.csproj -c Release -r win-x64 --self-contained false
```

Artefakt:

```text
ZgrzytDesktop\bin\Release\net10.0-windows\win-x64\publish\ZgrzytDesktop.exe
```

Na docelowym PC wymagany jest zainstalowany **.NET 10 Desktop Runtime** (przy `--self-contained false`).

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
├── ZgrzytDesktop/              # aplikacja Avalonia (MVVM, partial ViewModels)
│   ├── ViewModels/             # DashboardViewModel.*.cs (Navigation, Tickets, Admin, …)
│   ├── Constants/              # AppSections, AppRoles, statusy, …
│   └── Services/               # ApiService, AuthService, TicketService, …
├── ZgrzytDesktop.Tests/        # testy jednostkowe
├── .vscode/                    # launch.json, tasks.json
├── README_DESKTOP_STATUS.md
├── REQUIREMENTS.md
└── ZgrzytDesktop.sln
```

## Dane lokalne

Katalog: `%AppData%\ZgrzytDesktop\` — token, cache, audyt, ustawienia. Szczegóły i szyfrowanie: [README_DESKTOP_STATUS.md](README_DESKTOP_STATUS.md).
