# Testy integracyjne Zgrzyt Desktop ↔ API

Ten dokument opisuje **ręczne** sprawdzenie produkcyjnego API oraz **opcjonalne** testy automatyczne w projekcie `ZgrzytDesktop.Tests`.

## Adresy API

| Cel | URL | Uwagi |
|-----|-----|--------|
| **API desktopu (właściwy)** | `https://zgrzyt-api.onrender.com/api/` | Prefiks `/api/` — tak konfiguruje aplikacja desktopowa |
| Logowanie desktopu | `POST https://zgrzyt-api.onrender.com/api/login` | JSON: `login`, `password` |
| Panel admina (WWW) | `https://zgrzyt-api.onrender.com/admin/login` | **Nie** używać w kliencie desktop — to logowanie panelu webowego |

Domyślny lokalny adres w ustawieniach dev: `http://127.0.0.1:9000/api/`.

## Sekrety — nigdy w repozytorium

- **Nie** commituj loginu, hasła ani tokenów.
- **Nie** zapisuj plików `.env` z danymi logowania w repo (`.env` jest w `.gitignore`).
- Tokeny z odpowiedzi API trzymaj tylko lokalnie (np. zmienna `TOKEN` w sesji PowerShell na czas testu).

### Zmienne środowiskowe (testy automatyczne)

| Zmienna | Przykład | Opis |
|---------|----------|------|
| `ZGRZYT_API_URL` | `https://zgrzyt-api.onrender.com/api/` | Bazowy URL API (z lub bez `/api/` — aplikacja normalizuje) |
| `ZGRZYT_LOGIN` | *(twoje konto)* | Login użytkownika desktop |
| `ZGRZYT_PASSWORD` | *(twoje hasło)* | Hasło — tylko lokalnie / w CI secrets |

Ustawienie w PowerShell (sesja bieżąca):

```powershell
$env:ZGRZYT_API_URL = "https://zgrzyt-api.onrender.com/api/"
$env:ZGRZYT_LOGIN = "twoj_login"
$env:ZGRZYT_PASSWORD = "twoje_haslo"
```

Usunięcie z sesji:

```powershell
Remove-Item Env:ZGRZYT_API_URL, Env:ZGRZYT_LOGIN, Env:ZGRZYT_PASSWORD -ErrorAction SilentlyContinue
```

## Ręczne testy (curl / PowerShell)

Zastąp `LOGIN` i `PASSWORD` własnymi danymi. **Nie wklejaj ich do plików w repo.**

### 1. POST /api/login

```powershell
$base = "https://zgrzyt-api.onrender.com/api"
$body = @{ login = $env:ZGRZYT_LOGIN; password = $env:ZGRZYT_PASSWORD } | ConvertTo-Json
$response = Invoke-RestMethod -Method Post -Uri "$base/login" -Body $body -ContentType "application/json"
# Oczekiwane: access_token (lub access_token w JSON — zależnie od API), token_type Bearer
$token = $response.access_token
# Nie loguj $token w skryptach commitowanych do git
```

curl (Linux/macOS/Git Bash):

```bash
curl -sS -X POST "https://zgrzyt-api.onrender.com/api/login" \
  -H "Content-Type: application/json" \
  -d "{\"login\":\"${ZGRZYT_LOGIN}\",\"password\":\"${ZGRZYT_PASSWORD}\"}"
```

### 2. GET /api/user

```powershell
$headers = @{ Authorization = "Bearer $token" }
Invoke-RestMethod -Method Get -Uri "$base/user" -Headers $headers
```

```bash
curl -sS "https://zgrzyt-api.onrender.com/api/user" \
  -H "Authorization: Bearer $TOKEN"
```

### 3. GET /api/tickets

```powershell
Invoke-RestMethod -Method Get -Uri "$base/tickets?page=1&per_page=15" -Headers $headers
```

Warianty list (role staff): `GET /api/active-tickets`, `GET /api/unassigned-tickets`.

### 4. GET /api/users (admin / it)

Wymaga konta z rolą **admin** lub **it**.

```powershell
Invoke-RestMethod -Method Get -Uri "$base/users" -Headers $headers
```

Filtry list: `/api/active-users`, `/api/inactive-users`, `/api/banned-users`.

### 5. POST /api/logout

```powershell
Invoke-RestMethod -Method Post -Uri "$base/logout" -Headers $headers -Body "{}" -ContentType "application/json"
# Po wylogowaniu GET /api/user ze starym tokenem nie powinien zwrócić 200 OK.
# Oczekiwane: 401 Unauthorized lub 403 Forbidden.
# Na produkcji (Render) często występuje 500 InternalServerError zamiast 401 — znane niespójne zachowanie backendu (Sanctum), nie błąd desktopu.
```

## Testy automatyczne (opcjonalne)

Plik: `ZgrzytDesktop.Tests/Integration/LiveApiIntegrationTests.cs`  
Atrybut: `[Trait("Category", "Integration")]`

| Test | Endpoint(y) |
|------|-------------|
| `PostLogin_ReturnsAuthenticatedUser` | `POST /api/login`, profil użytkownika |
| `GetUser_ReturnsProfileAfterLogin` | `GET /api/user` |
| `GetTickets_ReturnsPaginatedList` | `GET /api/tickets` |
| `GetUsers_AsStaffRole_ReturnsUserList` | `GET /api/users` (pomijany dla roli `user`) |
| `PostLogout_InvalidatesBearerSession` | `POST /api/login`, `POST /api/logout`, potem `GET /api/user` ze starym tokenem → **nie** 200; akceptowane 401 / 403 / 500 (500 = znany kontrakt backendu) |

- Bez zmiennych środowiskowych testy są **pomijane** (Skipped), nie Failed.
- Testy **nie** wypisują tokenów w output.
- Konto `user` nie wywołuje `GET /api/users` (test staff jest pomijany z komunikatem).

### Uruchomienie

Tylko testy jednostkowe (domyślnie — integracja pominięta):

```powershell
dotnet test
```

Tylko integracja na żywym API:

```powershell
$env:ZGRZYT_API_URL = "https://zgrzyt-api.onrender.com/api/"
$env:ZGRZYT_LOGIN = "twoj_login"
$env:ZGRZYT_PASSWORD = "twoje_haslo"
dotnet test --filter "Category=Integration"
```

Wszystko (unit + headless + integracja, jeśli env ustawione):

```powershell
dotnet test
```

### Render.com — cold start

Przy pierwszym żądaniu po bezczynności API na Render może odpowiadać wolno (503/timeout). Testy integracyjne robią kilka ponowień logowania; przy ręcznych testach odczekaj ~30–60 s i powtórz żądanie.

## CI/CD

W pipeline ustaw `ZGRZYT_API_URL`, `ZGRZYT_LOGIN`, `ZGRZYT_PASSWORD` jako **secret variables** repozytorium (GitHub Actions / GitLab CI), nigdy w plaintext w YAML commitowanym publicznie.

Przykład GitHub Actions (fragment):

```yaml
env:
  ZGRZYT_API_URL: ${{ secrets.ZGRZYT_API_URL }}
  ZGRZYT_LOGIN: ${{ secrets.ZGRZYT_LOGIN }}
  ZGRZYT_PASSWORD: ${{ secrets.ZGRZYT_PASSWORD }}
run: dotnet test --filter "Category=Integration"
```

## Powiązane pliki

- `ZgrzytDesktop/Services/AuthService.cs` — login, user, logout, refresh
- `ZgrzytDesktop/Services/TicketService.cs` — tickets / active / unassigned
- `ZgrzytDesktop/Services/UserAdminService.cs` — users i listy admin
