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

$token = $response.access_token

```



### 2. GET /api/user



```powershell

$headers = @{ Authorization = "Bearer $token" }

Invoke-RestMethod -Method Get -Uri "$base/user" -Headers $headers

```



### 3. GET /api/tickets



```powershell

Invoke-RestMethod -Method Get -Uri "$base/tickets?page=1&per_page=15" -Headers $headers

```



Warianty list (role **it** / **admin**): `GET /api/active-tickets`, `GET /api/unassigned-tickets`.



Testy automatyczne wołają te endpointy z `page` i `per_page` tylko (bez `status` / `priority` / `sort_*`). Desktop przy filtrach kolejek stosuje **filtrowanie lokalne** po pobraniu (`TicketService`, `TicketQueueListProcessor`, `TicketQueuePageAggregator`).

**Limit bezpieczeństwa:** co najwyżej 50 stron × 100 zgłoszeń (5000 pozycji); pętla kończy się także przy pustej stronie lub `data.Count < per_page`. Przy limicie — komunikat `Tickets_QueueFetchTruncated` na pasku statusu listy.



### 4. GET /api/users (admin / it)



Wymaga konta z rolą **admin** lub **it**.



```powershell

Invoke-RestMethod -Method Get -Uri "$base/users" -Headers $headers

```



Filtry list: `/api/active-users`, `/api/inactive-users`, `/api/banned-users`. Jeśli któryś zwróci **404**, desktop pobiera `GET /api/users` i filtruje lokalnie (`UserAdminService`).



### 5. POST /api/logout i unieważnienie tokena



```powershell

Invoke-RestMethod -Method Post -Uri "$base/logout" -Headers $headers -Body "{}" -ContentType "application/json"

# Następnie GET /api/user ze STARYM tokenem (sprzed logout):

#   - NIE może zwrócić 200 OK z profilem użytkownika.

#   - Oczekiwane: 401 Unauthorized lub 403 Forbidden.

#   - Na produkcji (Render/Sanctum) często: 500 Internal Server Error zamiast 401.

```



#### Znane zachowanie backendu — logout / 500



| Krok | Endpoint | Typowy wynik | Uwagi |

|------|----------|--------------|--------|

| Wylogowanie | `POST /api/logout` | **2xx** (sukces) lub wyjątek 5xx | Desktop i tak czyści lokalny token w `finally` (`AuthService.LogoutAsync`) |

| Sprawdzenie sesji | `GET /api/user` ze **starym** Bearer | **401**, **403** lub **500** | **500** to znany bug/niespójność backendu, **nie** oznacza że token nadal działa |

| Bezpieczeństwo | `GET /api/user` ze starym Bearer | **Nigdy 200 OK** z profilem | Test integracyjny wymaga właśnie tego — 500 jest akceptowalne tylko gdy nie ma 200 |



**Podsumowanie:** 500 pojawia się zwykle na **`GET /api/user` po logout**, nie jako „fałszywy sukces” sesji. Sam `POST /api/logout` zwykle kończy się poprawnie; token jest unieważniony, jeśli kolejne `GET /api/user` nie zwraca profilu (200).



## Testy automatyczne (opcjonalne)



Plik: `ZgrzytDesktop.Tests/Integration/LiveApiIntegrationTests.cs`  

Atrybut: `[Trait("Category", "Integration")]`



### Rdzeń (5 testów)



| Test | Endpoint(y) | Uwagi |

|------|-------------|--------|

| `PostLogin_ReturnsAuthenticatedUser` | `POST /api/login` | Fixture loguje przy starcie klasy testowej |

| `GetUser_ReturnsProfileAfterLogin` | `GET /api/user` | |

| `GetTickets_ReturnsPaginatedList` | `GET /api/tickets` | Tylko odczyt, `page`/`per_page` |

| `GetUsers_AsStaffRole_ReturnsUserList` | `GET /api/users` | Pomijany gdy rola ≠ admin/it |

| `PostLogout_InvalidatesBearerSession` | `POST /api/logout`, potem `GET /api/user` ze starym tokenem | Izolowana sesja; **nie** modyfikuje danych produkcyjnych |



### Rozszerzenie (staff, gdy `ZGRZYT_LOGIN` ma rolę admin/it)



| Test | Endpoint(y) |

|------|-------------|

| `GetActiveTickets_AsStaffRole_ReturnsPaginatedList` | `GET /api/active-tickets` |

| `GetUnassignedTickets_AsStaffRole_ReturnsPaginatedList` | `GET /api/unassigned-tickets` |

| `GetStaffUserListEndpoints_ReturnsOkOrDocumentsNotFound` | `users`, `active-users`, `inactive-users`, `banned-users` (+ serwis desktop z fallbackiem 404) |



- Bez zmiennych środowiskowych: **wszystkie** testy integracyjne są **Skipped** (nie Failed).

- Testy **nie** wypisują tokenów w output.

- Konto `user`: testy staff są pomijane z komunikatem o wymaganej roli admin/it.

- Testy są **read-only** (poza logout w dedykowanej sesji tymczasowej).



### Uruchomienie



Tylko integracja:



```powershell

dotnet test -c Release --filter "Category=Integration"

```



Pełna paczka (unit + integracja jeśli env ustawione):



```powershell

dotnet test -c Release

```



Bez env (typowy CI / lokalnie):



| Projekt | Oczekiwany wynik |

|---------|------------------|

| `ZgrzytDesktop.Tests` | unit **passed**, integracja **skipped** (obecnie **8** testów `Category=Integration`) |

| `ZgrzytDesktop.Headless.Tests` | **passed** |



Z ustawionymi `ZGRZYT_*`: integracja **passed** lub **failed** z jasnym komunikatem (403 = zły login/rola, 200 po logout = problem bezpieczeństwa).



### Render.com — cold start



Przy pierwszym żądaniu po bezczynności API na Render może odpowiadać wolno (503/timeout). Fixture robi kilka ponowień logowania; przy ręcznych testach odczekaj ~30–60 s i powtórz żądanie.



## CI/CD



W pipeline ustaw `ZGRZYT_API_URL`, `ZGRZYT_LOGIN`, `ZGRZYT_PASSWORD` jako **secret variables** repozytorium (GitHub Actions / GitLab CI), nigdy w plaintext w YAML commitowanym publicznie.



Przykład GitHub Actions (fragment):



```yaml

env:

  ZGRZYT_API_URL: ${{ secrets.ZGRZYT_API_URL }}

  ZGRZYT_LOGIN: ${{ secrets.ZGRZYT_LOGIN }}

  ZGRZYT_PASSWORD: ${{ secrets.ZGRZYT_PASSWORD }}

run: dotnet test -c Release --filter "Category=Integration"

```



## Powiązane pliki



- `ZgrzytDesktop/Services/AuthService.cs` — login, user, logout

- `ZgrzytDesktop/Services/TicketService.cs` — tickets / active / unassigned

- `ZgrzytDesktop/Helpers/TicketQueueListProcessor.cs` — filtr/sort lokalny kolejek

- `ZgrzytDesktop/Services/UserAdminService.cs` — users i listy admin

- `ZgrzytDesktop.Tests/Infrastructure/IntegrationApiTestHost.cs` — fixture live API

- `ZgrzytDesktop.Tests/Infrastructure/LiveApiTestHelpers.cs` — asercje kontraktu (logout 401/403/500)

