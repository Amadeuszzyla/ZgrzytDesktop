# Requirements — ZGRZYT Desktop

## 1. System

Aplikacja desktopowa jest przeznaczona na **Windows 10/11** dla personelu **IT** i **administratorów**.

Projekt: `net10.0-windows` (Avalonia Desktop). Konto z rolą **`user`** nie uzyskuje dostępu do dashboardu po zalogowaniu.

## 2. Wymagane narzędzia (rozwój)

- Windows 10 lub Windows 11
- [.NET SDK 10](https://dotnet.microsoft.com/download) zgodny z `net10.0-windows`
- JetBrains Rider lub Visual Studio (opcjonalnie VS Code)
- Działające API ZGRZYT w trybie online

## 3. Backend API

Domyślny adres produkcyjny (kod / `settings.json`, bez edycji URL w UI ustawień):

```text
https://zgrzyt-api.onrender.com/api/
```

Lokalny development: `http://127.0.0.1:9000/api/`

### Uwierzytelnianie i konta

- Logowanie: `POST /api/login` (Bearer / Sanctum), `GET /api/user`, `POST /api/logout`, `POST /api/refresh`
- **Tworzenie kont przez Admin/IT:** `POST /api/register` (Administracja → Nowe konto; pola: `name`, `login`, `email`, `password`, `password_confirmation`, `role`)
- **Prośba o konto:** `POST /api/request-account` — osobny flow, nie zastępuje rejestracji staff

### Zgłoszenia i audyt

- Listy zgłoszeń z paginacją Laravel (`GET /api/tickets` i kolejki staff)
- Przypisanie zgłoszenia: `PUT /api/tickets/{id}` z polem `assigned_it_id` (admin: wybór IT/admin; IT: „Przypisz do mnie”)
- Wiadomości: `GET` / `POST .../messages`, pole JSON `body` — **odczyt i dodawanie**
- **Edycja i usuwanie pojedynczych wiadomości** wymagają dedykowanych endpointów backendowych (`PUT`/`PATCH`/`DELETE` na `.../messages/{id}`); desktop **nie symuluje** tych operacji lokalnie
- **Brak `GET /api/logs`** w API — desktop prowadzi **lokalny audyt** (plik w AppData, **DPAPI**); historia zmian w szczegółach zgłoszenia jest lokalna
- **Statystyki:** liczba zgłoszeń, statusy, priorytety, przypisania; bez czasu pierwszej reakcji w UI (API nie dostarcza stabilnych danych)

### Interfejs

- Język: **polski** / **angielski**
- Motyw: **wyłącznie jasny** (light-only) — brak dark/system w UI

## 4. Runtime na maszynie docelowej (publish)

Paczka **self-contained** (ZIP z `scripts/publish-release.ps1`) **nie wymaga** instalacji .NET Desktop Runtime na PC użytkownika.

Użytkownik uruchamia `ZgrzytDesktop.exe` z **całego** rozpakowanego folderu publish.

## 5. Dane lokalne

`%AppData%\ZgrzytDesktop\` — token, cache i audyt chronione **DPAPI** (CurrentUser); ustawienia UI w `Settings/settings.json` jako zwykły JSON (preferencje bez haseł/tokenów). Aplikacja nie wymaga uprawnień administratora Windows.

Szczegóły: [README.md](README.md).

## 6. Zabezpieczenia po stronie aplikacji desktopowej

### Obecnie (zaimplementowane)

- **Uwierzytelnianie tokenem** — `POST /api/login`, Bearer w DPAPI (`token.txt`), `GET /api/user`, `POST /api/logout`, `POST /api/refresh`
- **Dostęp do desktopa** — tylko role **IT** i **admin**; rola **`user`** nie wchodzi do dashboardu (`DesktopAccessHelper`)
- **Role-based UI** — admin: zarządzanie użytkownikami; IT: rejestracja kont (**Nowe konto**); ukrywanie niedostępnych sekcji
- **DPAPI** — token, cache zgłoszeń, cache użytkownika, lokalny audyt w `%AppData%\ZgrzytDesktop\`
- **Ustawienia (plaintext)** — `Settings/settings.json` przez `SettingsService`; model `AppSettings` nie zawiera pól wrażliwych (hasła, tokeny)
- **Sanityzacja HTML** — plain text w tytule/opisie zgłoszenia i wiadomościach; filtrowanie odpowiedzi HTML z API
- **Brak haseł/tokenów w logach lokalnych** — maskowanie wrażliwych danych w audycie i błędach API
- **Błędy bez stack trace** — zlokalizowane komunikaty (`ApiErrorSanitizer`, `LoginErrorMapper`)
- **HTTPS dla API produkcyjnego** — domyślny URL `https://…`; `ApiUrlSecurityHelper` podnosi zdalne `http://` do HTTPS
- **Wylogowanie** — usuwa token i cache sesji; nie usuwa preferencji języka z ustawień
- **Auto-wylogowanie po bezczynności** — `SessionInactivityMonitor` w `MainWindowViewModel` śledzi aktywność użytkownika i po przekroczeniu limitu wywołuje `LogoutAsync` z komunikatem `Security_SessionExpiredInactivity`; włączenie/wyłączenie i timeout (15 / 30 / 60 min) przez `AppSettings.AutoLogoutEnabled` i `AppSettings.AutoLogoutTimeoutMinutes` w `Settings/settings.json` (domyślnie włączone, 30 min); zmiana ustawień jest stosowana przy starcie sesji dashboardu i po zapisie ustawień — panel **Ustawienia** w UI obecnie eksponuje język i odświeżenie sesji, bez osobnych kontrolek auto-wylogowania
- **Wiadomości** — tylko odczyt i dodawanie; **bez** lokalnej edycji/usuwania

### Przyszłe rozszerzenia (poza obecnym zakresem)

- Edycja/usuwanie pojedynczych wiadomości (wymaga endpointów backendowych)
- Pełny audyt serwerowy (`GET /api/logs` lub równoważny)
- Rate limiting po stronie backendu
- Rotacja tokenów po stronie backendu

**Ograniczenia wymagające backendu:** wymuszenie TLS, timeout sesji API, autoryzacja endpointów, walidacja po stronie serwera.
