# Bezpieczeństwo — ZGRZYT Desktop

Ten dokument opisuje zasady zgłaszania podatności, lokalne przechowywanie danych oraz ograniczenia klienta desktopowego.

## Zgłaszanie podatności

Jeśli odkryjesz problem bezpieczeństwa:

1. **Nie publikuj** szczegółów publicznie (issue, PR, social media) przed uzgodnieniem.
2. Zgłoś problem **prywatnie** — preferowany kanał: [GitHub Security Advisories](https://docs.github.com/en/code-security/security-advisories/guidance-on-reporting-and-writing-information-about-vulnerabilities/privately-reporting-a-security-vulnerability) dla tego repozytorium.
3. Dołącz: opis problemu, kroki reprodukcji, wersję aplikacji / commit, ocenę wpływu oraz ewentualną propozycję naprawy.
4. Odpowiemy w rozsądnym terminie i uzgodnimy harmonogram publikacji poprawki.

Nie prowadzimy programu bug bounty. Doceniamy jednak odpowiedzialne zgłoszenia.

## Dane przechowywane lokalnie

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

## Ochrona danych lokalnych

| Zasób | Ochrona |
|-------|---------|
| Token sesji | **Windows DPAPI** (`DataProtectionScope.CurrentUser`) |
| Cache zgłoszeń i użytkownika | **DPAPI** |
| Lokalny audyt | **DPAPI** |
| Ustawienia (`settings.json`) | Plaintext JSON — **bez haseł i tokenów** |

DPAPI wiąże dane z bieżącym kontem Windows. Chroni przed odczytem przez innych użytkowników tego samego komputera, ale **nie** przed oprogramowaniem działającym w kontekście zalogowanego użytkownika ani przed pełnym dostępem do dysku (administrator, malware, backup).

## Czego klient desktopowy nie gwarantuje

Bez współpracy i poprawnej konfiguracji **backendu API** aplikacja nie może zapewnić:

- weryfikacji tożsamości i uprawnień po stronie serwera,
- integralności i poufności danych przechowywanych w bazie API,
- ochrony przed manipulacją żądaniami HTTP (np. modyfikacja klienta, proxy, replay),
- unieważnienia skradzionego tokena (revocation) — zależy od polityki API,
- zgodności z politykami organizacji (SIEM, DLP, MDM) — to warstwa wdrożeniowa poza samym EXE.

Ukrycie przycisku lub panelu w UI **nie stanowi** kontroli bezpieczeństwa.

## Autoryzacja po stronie API

**Każde żądanie do API musi być autoryzowane i autentykowane po stronie serwera** — na podstawie tokena, roli i reguł biznesowych backendu.

Klient desktop:

- wysyła token w nagłówku `Authorization`,
- ukrywa elementy UI według roli (IT, admin, user),

ale **nie decyduje** o dostępie do danych. Endpointy API muszą odrzucać nieautoryzowane operacje niezależnie od tego, czy użytkownik zmodyfikował aplikację lub wysłał żądanie spoza UI.

## Sekrety w repozytorium i CI

| Zasada | Szczegóły |
|--------|-----------|
| Nie commituj `.env` | Plik jest w `.gitignore`. Wzorzec: [.env.example](.env.example) |
| Nie commituj tokenów, haseł, kluczy API | Ani w kodzie, ani w testach, ani w logach CI |
| Testy integracyjne | Używaj **zmiennych środowiskowych**: `ZGRZYT_API_URL`, `ZGRZYT_LOGIN`, `ZGRZYT_PASSWORD` — patrz [INTEGRATION_TESTS.md](INTEGRATION_TESTS.md) |
| CI / pipeline | Sekrety jako **GitHub Actions Secrets** (lub odpowiednik), nie w commitowanym YAML |

Domyślny `dotnet test` w CI **pomija** testy `Category=Integration`, dopóki nie skonfigurujesz sekretów świadomie.

## Bezpieczeństwo release

| Obszar | Zalecenie |
|--------|-----------|
| **HTTPS** | Produkcyjne API **musi** używać HTTPS. Domyślny URL w aplikacji wskazuje na endpoint HTTPS. |
| **Podpisywanie aplikacji** | **Rekomendowane** dla wdrożeń organizacyjnych (Authenticode) — zmniejsza ryzyko fałszywych instalatorów. |
| **Checksumy release** | **Rekomendowane** — publikuj SHA-256 archiwum ZIP obok paczki, aby użytkownicy mogli zweryfikować integralność pobranego pliku. |

Skrypt publikacji: [`scripts/publish-release.ps1`](scripts/publish-release.ps1). Instrukcja dla użytkownika końcowego: [README_RELEASE.txt](README_RELEASE.txt).

## Powiązane dokumenty

- [README.md](README.md) — sekcja „Dane lokalne”
- [REQUIREMENTS.md](REQUIREMENTS.md) — wymagania środowiska
- [INTEGRATION_TESTS.md](INTEGRATION_TESTS.md) — bezpieczna konfiguracja testów live API
