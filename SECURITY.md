# Bezpieczeństwo — ZgrzytDesktop

Ten dokument opisuje zgłaszanie podatności, lokalne przechowywanie danych, ograniczenia klienta desktopowego oraz praktyki release.

## 1. Zgłaszanie podatności

Jeśli odkryjesz problem bezpieczeństwa:

1. **Nie publikuj** szczegółów publicznie (issue, PR, social media) przed uzgodnieniem.
2. Zgłoś problem **prywatnie** — preferowany kanał: [GitHub Security Advisories](https://docs.github.com/en/code-security/security-advisories/guidance-on-reporting-and-writing-information-about-vulnerabilities/privately-reporting-a-security-vulnerability) dla tego repozytorium.
3. Dołącz: opis problemu, kroki reprodukcji, wersję aplikacji / commit, ocenę wpływu oraz ewentualną propozycję naprawy.
4. Odpowiemy w rozsądnym terminie i uzgodnimy harmonogram publikacji poprawki.

Nie prowadzimy programu bug bounty. Doceniamy jednak odpowiedzialne zgłoszenia.

## 2. Dane lokalne aplikacji

Aplikacja zapisuje dane w katalogu użytkownika Windows:

`%AppData%\ZgrzytDesktop\`

| Plik | Zawartość |
|------|-----------|
| `token.txt` | Token sesji (Bearer JWT) po zalogowaniu |
| `Cache/tickets-cache.json` | Cache zgłoszeń (tytuły, opisy, metadane) |
| `Cache/user-cache.json` | Profil zalogowanego użytkownika |
| `audit-log.json` | Lokalny dziennik audytu (akcje w aplikacji) |
| `Settings/settings.json` | Preferencje UI: język, motyw, auto-wylogowanie, adres API |

**Hasła logowania nie są zapisywane** na dysku. Po zalogowaniu przechowywany jest wyłącznie token zwrócony przez API.

## 3. Ochrona lokalnych danych

| Zasób | Ochrona |
|-------|---------|
| Token sesji (`token.txt`) | **Windows DPAPI** (`DataProtectionScope.CurrentUser`) |
| Cache zgłoszeń i użytkownika | **DPAPI** |
| Lokalny audyt (`audit-log.json`) | **DPAPI** |
| Ustawienia (`settings.json`) | Plaintext JSON — **bez haseł i tokenów** |

DPAPI wiąże dane z bieżącym kontem Windows. Chroni przed odczytem przez innych użytkowników tego samego komputera, ale **nie** przed oprogramowaniem działającym w kontekście zalogowanego użytkownika ani przed pełnym dostępem do dysku (administrator, malware, backup).

## 4. Zabezpieczenia aplikacji desktopowej

| Mechanizm | Opis |
|-----------|------|
| **Bearer token** | `POST /api/login`, token w DPAPI, `POST /api/refresh`, wylogowanie lokalne i przez API |
| **Role IT / admin / user** | Dashboard tylko dla IT i admin; rola `user` jest wylogowywana po logowaniu |
| **Role-based UI** | Admin: zarządzanie użytkownikami; IT: rejestracja kont; niedostępne sekcje są ukryte |
| **Sanityzacja HTML** | `HtmlTextSanitizer` — tytuł/opis zgłoszenia i wiadomości; odrzucanie HTML z API jako błędów |
| **Maskowanie w logach** | `SensitiveDataMasker`, `SensitiveDataRedactor` — brak haseł/tokenów w audycie i błędach |
| **Brak stack trace w UI** | Zlokalizowane komunikaty (PL/EN); stack trace nie trafia do użytkownika |
| **Auto-wylogowanie** | `SessionInactivityMonitor` — wylogowanie po bezczynności (konfigurowalny timeout) |
| **HTTPS** | Domyślny URL produkcyjny używa `https://`; zdalne `http://` jest podnoszone do HTTPS (`ApiUrlSecurityHelper`) |

## 5. API i ograniczenia

- **Ostateczna autoryzacja musi być po stronie backendu** — każde żądanie weryfikuje token, rolę i reguły biznesowe serwera.
- **Ukrycie przycisku w UI nie jest zabezpieczeniem** — zmodyfikowany klient lub bezpośrednie żądanie HTTP musi być odrzucone przez API.
- **Poza zakresem desktopu:** rate limiting, pełna rotacja/revocation tokenów po stronie serwera, pełny audyt serwerowy (`GET /api/logs`), wymuszenie TLS po stronie infrastruktury.

Klient desktop wysyła token w nagłówku `Authorization` i ukrywa elementy UI według roli, ale **nie decyduje** o dostępie do danych.

## 6. Sekrety

| Zasada | Szczegóły |
|--------|-----------|
| Nie commituj `.env` | Plik jest w `.gitignore`. Wzorzec: [.env.example](.env.example) |
| Zmienne testów integracyjnych | `ZGRZYT_API_URL`, `ZGRZYT_LOGIN`, `ZGRZYT_PASSWORD` — tylko lokalnie lub jako **GitHub Actions Secrets** |
| Nie loguj haseł/tokenów | Ani w kodzie, ani w testach, ani w logach CI |
| Domyślny CI | `dotnet test --filter "Category!=Integration"` — testy live pomijane bez sekretów |

PowerShell (sesja bieżąca):

```powershell
$env:ZGRZYT_API_URL = "https://zgrzyt-api.onrender.com/api/"
$env:ZGRZYT_LOGIN = "twoj_login"
$env:ZGRZYT_PASSWORD = "twoje_haslo"
```

## 7. Release security

| Obszar | Stan |
|--------|------|
| **SHA256** | Workflow release generuje `.sha256` dla instalatora, deinstalatora i portable ZIP |
| **Authenticode** | **Nie skonfigurowany** — brak podpisu cyfrowego EXE/instalatora |
| **SmartScreen** | Windows może pokazać ostrzeżenie przy pierwszym uruchomieniu niepodpisanego pliku |
| **Deinstalator** | `ZgrzytDesktopUninstall.exe` **nie usuwa plików ręcznie** — szuka `unins000.exe` w rejestrze Windows (AppId / DisplayName) z fallbackiem do `%LocalAppData%\Programs\ZgrzytDesktop\` i uruchamia deinstalator Inno Setup |

Skrypt publikacji lokalnej: [`scripts/publish-release.ps1`](scripts/publish-release.ps1). Build release w CI: [`.github/workflows/release.yml`](.github/workflows/release.yml).

## 8. Checklist bezpieczeństwa przed oddaniem

- [ ] Brak sekretów, haseł i tokenów w repozytorium (w tym `.env`, logi, testy)
- [ ] Brak haseł i tokenów w lokalnym audycie i komunikatach błędów
- [ ] `dotnet test ZgrzytDesktop.sln -c Release --filter "Category!=Integration"` przechodzi
- [ ] Artefakty release mają pliki `.sha256`
- [ ] Aplikacja używa HTTPS dla produkcyjnego API
- [ ] Testy integracyjne live uruchamiane tylko ze świadomie skonfigurowanymi sekretami

## Powiązane

- [README.md](README.md) — architektura, testy, CI/CD, uruchomienie release
