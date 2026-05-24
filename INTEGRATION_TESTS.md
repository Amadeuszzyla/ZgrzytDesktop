# Testy integracyjne — ZGRZYT Desktop ↔ API

Opcjonalne testy automatyczne i wskazówki do ręcznej weryfikacji API produkcyjnego.

## Adres API

| Cel | URL |
|-----|-----|
| API desktopu | `https://zgrzyt-api.onrender.com/api/` |
| Development (lokalnie) | `http://127.0.0.1:9000/api/` |

Panel WWW (`/admin/login`) nie jest używany przez klienta desktop.

## Zmienne środowiskowe

| Zmienna | Przykład | Opis |
|---------|----------|------|
| `ZGRZYT_API_URL` | `https://zgrzyt-api.onrender.com/api/` | Bazowy URL (z lub bez końcówki `/api/`) |
| `ZGRZYT_LOGIN` | *(twoje konto)* | Login — **wymagana rola IT lub admin** |
| `ZGRZYT_PASSWORD` | *(hasło)* | Tylko lokalnie / sekrety CI — **nie commituj** |

PowerShell (sesja bieżąca):

```powershell
$env:ZGRZYT_API_URL = "https://zgrzyt-api.onrender.com/api/"
$env:ZGRZYT_LOGIN = "twoj_login"
$env:ZGRZYT_PASSWORD = "twoje_haslo"
```

Wzorzec w repozytorium: [.env.example](.env.example)

## Smoke test (PowerShell, ręcznie)

Skrypt [`scripts/smoke-live-api.ps1`](scripts/smoke-live-api.ps1) sprawdza produkcyjne API bez crasha przy braku `WebException.Response` (timeout, DNS, TLS, cold start Render).

Domyślny URL: `https://zgrzyt-api.onrender.com/api/` — nadpisanie przez `ZGRZYT_API_URL`.

```powershell
cd C:\ścieżka\do\ZgrzytDesktop

# opcjonalnie (sesja bieżąca)
$env:ZGRZYT_API_URL = "https://zgrzyt-api.onrender.com/api/"
$env:ZGRZYT_LOGIN = "twoj_login"
$env:ZGRZYT_PASSWORD = "twoje_haslo"

powershell -ExecutionPolicy Bypass -File .\scripts\smoke-live-api.ps1
```

| Test | Zachowanie |
|------|------------|
| **GET `/api/user`** (bez tokena) | **401 → PASS** — API żyje i wymaga auth; **brak odpowiedzi HTTP → INFO/FAIL** (połączenie, cold start); **500+ → FAIL** backend |
| **POST `/api/login`** | Wykonywany tylko gdy ustawione **`ZGRZYT_LOGIN`** i **`ZGRZYT_PASSWORD`**; w przeciwnym razie: *Login smoke skipped because ZGRZYT_LOGIN/ZGRZYT_PASSWORD are not set.* |

Login smoke (gdy env ustawione): **200 → PASS**; **401 → FAIL** (invalid credentials); **403 → FAIL** (brak dostępu); **422 → FAIL** (validation); **500+ → FAIL** (backend); brak HTTP → FAIL (connection).

Skrypt **nie loguje** hasła ani tokena (`access_token` w podglądzie body jest redagowany).

Kod wyjścia: **0** gdy wszystkie wykonane testy przeszły; **1** przy co najmniej jednym FAIL.

## Uruchomienie

Tylko testy integracyjne:

```powershell
dotnet test -c Release --filter "Category=Integration"
```

Pełna paczka testów:

```powershell
dotnet test -c Release
```

**Bez ustawionych zmiennych:** testy `Category=Integration` są **Skipped** (nie Failed).

**Z ustawionymi zmiennymi:** wymagane konto **IT** lub **admin**; testy staff są pomijane dla roli `user`.

## Zakres testów live

Plik: `ZgrzytDesktop.Tests/Integration/LiveApiIntegrationTests.cs`

Testy są **read-only** (poza `POST /api/logout` w izolowanej sesji). **Nie** tworzą zgłoszeń ani kont na produkcji.

| Test | Endpoint |
|------|----------|
| `PostLogin_ReturnsAuthenticatedUser` | `POST /api/login` |
| `GetUser_ReturnsProfileAfterLogin` | `GET /api/user` |
| `GetTickets_ReturnsPaginatedList` | `GET /api/tickets` (`page`, `per_page`) |
| `GetUsers_AsStaffRole_ReturnsUserList` | `GET /api/users` |
| `PostLogout_InvalidatesBearerSession` | `POST /api/logout` + `GET /api/user` starym tokenem |
| `GetActiveTickets_AsStaffRole_ReturnsPaginatedList` | `GET /api/active-tickets` |
| `GetUnassignedTickets_AsStaffRole_ReturnsPaginatedList` | `GET /api/unassigned-tickets` |
| `GetStaffUserListEndpoints_ReturnsOkOrDocumentsNotFound` | listy użytkowników |

### `POST /api/register`

Brak testu live — rejestracja tworzyłaby konta na współdzielonym API. Kontrakt weryfikują testy mock: `RegisterUserTests`, `UserAdminServiceTests`.

## Znane ograniczenia backendu

### Wylogowanie

Po `POST /api/logout` kolejne `GET /api/user` **starym** tokenem Bearer:

- **Nie** może zwracać **200 OK** z profilem.
- Akceptowane: **401**, **403** lub **500** (znana niespójność Sanctum/Render — 500 **nie** oznacza ważnej sesji).

Desktop i tak czyści lokalny token w `finally` (`AuthService.LogoutAsync`).

### Cold start (Render)

Pierwsze żądanie po bezczynności może trwać dłużej. Przy timeoutach odczekaj ~30–60 s i powtórz.

## CI/CD

Ustaw `ZGRZYT_*` jako **secrets** w pipeline (GitHub Actions / GitLab CI), nie w commitowanym YAML.

```yaml
env:
  ZGRZYT_API_URL: ${{ secrets.ZGRZYT_API_URL }}
  ZGRZYT_LOGIN: ${{ secrets.ZGRZYT_LOGIN }}
  ZGRZYT_PASSWORD: ${{ secrets.ZGRZYT_PASSWORD }}
run: dotnet test -c Release --filter "Category=Integration"
```

## Powiązane pliki

- `scripts/smoke-live-api.ps1`
- `ZgrzytDesktop/Services/AuthService.cs`
- `ZgrzytDesktop/Services/TicketService.cs`
- `ZgrzytDesktop/Services/UserAdminService.cs`
- `ZgrzytDesktop.Tests/Infrastructure/IntegrationApiTestHost.cs`
