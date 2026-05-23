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
- Wiadomości: pole JSON `body`
- **Brak `GET /api/logs`** w API — desktop prowadzi **lokalny audyt** (plik w AppData, **DPAPI**); historia zmian w szczegółach zgłoszenia jest lokalna
- **Statystyki:** liczba zgłoszeń, statusy, priorytety, przypisania; bez czasu pierwszej reakcji w UI (API nie dostarcza stabilnych danych)

### Interfejs

- Język: **polski** / **angielski**
- Motyw: **wyłącznie jasny** (light-only) — brak dark/system w UI

## 4. Runtime na maszynie docelowej (publish)

Paczka **self-contained** (ZIP z `scripts/publish-release.ps1`) **nie wymaga** instalacji .NET Desktop Runtime na PC użytkownika.

Użytkownik uruchamia `ZgrzytDesktop.exe` z **całego** rozpakowanego folderu publish.

## 5. Dane lokalne

`%AppData%\ZgrzytDesktop\` — token, cache, audyt, ustawienia; ochrona **DPAPI** (CurrentUser). Aplikacja nie wymaga uprawnień administratora Windows.

Szczegóły: [README.md](README.md).
